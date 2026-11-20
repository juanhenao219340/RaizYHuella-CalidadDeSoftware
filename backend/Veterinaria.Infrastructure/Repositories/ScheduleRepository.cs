using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Configuration;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;
using Veterinaria.Domain.ValueObjects;
using Veterinaria.Infrastructure.Persistence;
using Veterinaria.Infrastructure.Repositories.Concurrency;

namespace Veterinaria.Infrastructure.Repositories;

// Sin SQL escrito a mano (RNF-ARQ-07). Excepción única R13-3: el consecutivo vive en ReferenceNumberSequenceCounter.
// Turno por franja (R13-4): reservar, reprogramar y anular toman la fila SlotControls con ExecuteUpdateAsync dentro de la transacción.
public sealed class ScheduleRepository(
    VeterinaryDbContext context,
    IAppointmentRepository appointments,
    ReferenceNumberSequenceCounter sequenceCounter) : IScheduleRepository
{
    public async Task<Appointment> BookAsync(
        AppointmentRequest request,
        Guid ownerId,
        DateTimeOffset nowUtc,
        BusinessRulesOptions options,
        CancellationToken cancellationToken)
    {
        // Authoritative checks on deleted records (LINQ, no row locks). Run before the slot row is created so a missing service never reaches the FK.
        if (await PetDeletedFlagAsync(request.PetId, cancellationToken) is not false)
        {
            throw new DomainRuleException("RF-REG-10", "La mascota está eliminada y no puede usarse para nuevas citas.");
        }

        if (await ServiceDeletedFlagAsync(request.ServiceId, cancellationToken) is not false)
        {
            throw new DomainRuleException("RF-REG-10", "El servicio está eliminado y no puede usarse para nuevas citas.");
        }

        if (await OwnerDeletedFlagAsync(ownerId, cancellationToken) is not false)
        {
            throw new InactiveAccountException();
        }

        var slot = new SlotKey(request.Date, request.StartTime, request.ServiceId);
        await EnsureSlotControlAsync(slot, cancellationToken);

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        // Waits for any reschedule or cancel that holds this slot. Counts after the lock, so it sees their committed result.
        await TakeSlotsAsync([slot], cancellationToken);

        var relevant = await LoadRelevantAsync(request.Date, request.StartTime, request.ServiceId, request.PetId, cancellationToken);

        SchedulingRules.Validate(request, nowUtc, options, relevant, null).ThrowIfFailed();

        var registrationDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(nowUtc, options.GetTimeZone()).DateTime);
        var sequence = await sequenceCounter.NextAsync(registrationDate, cancellationToken);
        var reference = ReferenceNumberGenerator.Format(registrationDate, sequence);

        var appointment = Appointment.Schedule(Guid.NewGuid(), reference, request, nowUtc, options, relevant);

        context.Appointments.Add(appointment);
        await PersistenceErrors.SaveChangesAsync(context, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return appointment;
    }

    public async Task<Appointment?> RescheduleAsync(
        Guid appointmentId,
        RescheduleRequest request,
        UserRole actorRole,
        string? reason,
        DateTimeOffset nowUtc,
        BusinessRulesOptions options,
        CancellationToken cancellationToken)
    {
        // Existence check (LINQ, no tracking). Returns null when the appointment does not exist, as before.
        var origin = await FindAppointmentSnapshotAsync(appointmentId, cancellationToken);

        if (origin is null)
        {
            return null;
        }

        // An appointment already cancelled at the time of the operation is RF-PRO-07 (R13-13).
        // A cancellation that happens after this snapshot is a version conflict (RF-PRO-11) via EnsureSameVersion.
        if (origin.Value.Status == AppointmentStatus.Cancelled)
        {
            throw new DomainRuleException(PersistenceErrors.AppointmentCancelledCode, PersistenceErrors.AppointmentCancelledMessage);
        }

        // Only the resulting service is validated (D-B). Pet and owner may be deleted.
        var serviceDeleted = await ServiceDeletedFlagAsync(request.ServiceId, cancellationToken);

        if (serviceDeleted is null)
        {
            return null;
        }

        if (serviceDeleted is true)
        {
            throw new DomainRuleException("RF-REG-10", "El servicio está eliminado y no puede usarse para nuevas citas.");
        }

        var destination = new SlotKey(request.Date, request.StartTime, request.ServiceId);
        await EnsureSlotControlAsync(origin.Value.Slot, cancellationToken);
        await EnsureSlotControlAsync(destination, cancellationToken);

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        // Origin and destination in fixed order to avoid deadlocks between crossed reschedules (plan step 6).
        await TakeSlotsAsync([origin.Value.Slot, destination], cancellationToken);

        // Tracked load taken under the turn. Its version is the one checked on save (R13-7).
        var existing = await LoadFreshAsync(appointmentId, cancellationToken);

        if (existing is null)
        {
            return null;
        }

        EnsureSameVersion(existing, origin.Value);
        EnsureSameSlot(existing, origin.Value.Slot);

        var relevant = await LoadRelevantAsync(request.Date, request.StartTime, request.ServiceId, existing.PetId, cancellationToken);

        var updated = new AppointmentRequest(existing.PetId, request.Date, request.StartTime, request.ServiceId);

        // The appointment's own slot is excluded from the count inside AppointmentModificationRules.ValidateUpdate (plan step 8).
        existing.Update(actorRole, reason, nowUtc, updated, nowUtc, options, relevant);

        await PersistenceErrors.SaveChangesAsync(context, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return existing;
    }

    public async Task<Appointment?> CancelAsync(
        Guid appointmentId,
        UserRole actorRole,
        string? reason,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var origin = await FindAppointmentSnapshotAsync(appointmentId, cancellationToken);

        if (origin is null)
        {
            return null;
        }

        await EnsureSlotControlAsync(origin.Value.Slot, cancellationToken);

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        await TakeSlotsAsync([origin.Value.Slot], cancellationToken);

        // Tracked load taken under the turn, so the checks use the confirmed state.
        var existing = await LoadFreshAsync(appointmentId, cancellationToken);

        if (existing is null)
        {
            return null;
        }

        EnsureSameVersion(existing, origin.Value);
        EnsureSameSlot(existing, origin.Value.Slot);

        existing.Cancel(actorRole, reason, nowUtc);

        await PersistenceErrors.SaveChangesAsync(context, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return existing;
    }

    // Tracked load for the business checks. Status and Version are the concurrency tokens checked on save.
    private Task<Appointment?> LoadFreshAsync(Guid appointmentId, CancellationToken cancellationToken) =>
        context.Appointments.FirstOrDefaultAsync(appointment => appointment.Id == appointmentId, cancellationToken);

    // Snapshot of the appointment as currently stored, read without tracking before the transaction.
    // Slot creates the slot row; Version and Status are compared with the tracked load taken under the turn.
    private async Task<AppointmentSnapshot?> FindAppointmentSnapshotAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var row = await context.Appointments
            .AsNoTracking()
            .Where(appointment => appointment.Id == appointmentId)
            .Select(appointment => new { appointment.Date, appointment.StartTime, appointment.ServiceId, appointment.Version, appointment.Status })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new AppointmentSnapshot(new SlotKey(row.Date, row.StartTime, row.ServiceId), row.Version, row.Status);
    }

    // The slot row is created in its own operation, before the transaction (R13-4, plan step 4).
    // Appointments created before T-R13-05 have no row yet, so origin rows are created here as well.
    private async Task EnsureSlotControlAsync(SlotKey slot, CancellationToken cancellationToken)
    {
        if (await SlotControlExistsAsync(slot, cancellationToken))
        {
            return;
        }

        var control = SlotControl.Create(slot.Date, slot.StartTime, slot.ServiceId);
        context.SlotControls.Add(control);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another operation created the row first. Detach the failed insert and look the row up once.
            context.Entry(control).State = EntityState.Detached;

            if (!await SlotControlExistsAsync(slot, cancellationToken))
            {
                throw new InvalidOperationException($"Slot control row is missing after a concurrent insert for {slot.Date:yyyy-MM-dd} {slot.StartTime}.");
            }
        }
    }

    private Task<bool> SlotControlExistsAsync(SlotKey slot, CancellationToken cancellationToken) =>
        context.SlotControls
            .AsNoTracking()
            .AnyAsync(control => control.Date == slot.Date
                && control.StartTime == slot.StartTime
                && control.ServiceId == slot.ServiceId, cancellationToken);

    // Takes the turn of each distinct slot in fixed order. The UPDATE holds the row lock until commit or rollback (R13-4).
    private async Task TakeSlotsAsync(IEnumerable<SlotKey> slots, CancellationToken cancellationToken)
    {
        foreach (var slot in slots.Distinct().OrderBy(key => key.Date).ThenBy(key => key.StartTime.StartTime).ThenBy(key => key.ServiceId))
        {
            var taken = await context.SlotControls
                .Where(control => control.Date == slot.Date
                    && control.StartTime == slot.StartTime
                    && control.ServiceId == slot.ServiceId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(control => control.Version, control => control.Version + 1), cancellationToken);

            if (taken == 0)
            {
                throw new InvalidOperationException($"Slot control row is missing for {slot.Date:yyyy-MM-dd} {slot.StartTime}.");
            }
        }
    }

    // The version and status read before the turn must match the tracked load taken under it. Any change in between is a version conflict (RF-PRO-11).
    private static void EnsureSameVersion(Appointment existing, AppointmentSnapshot captured)
    {
        if (existing.Version != captured.Version || existing.Status != captured.Status)
        {
            throw new DomainRuleException(PersistenceErrors.ConcurrencyCode, PersistenceErrors.AppointmentModifiedMessage);
        }
    }

    // The slot must still be the stored slot once the turn is held. If another operation moved the appointment, report a version conflict (RF-PRO-11).
    private static void EnsureSameSlot(Appointment existing, SlotKey origin)
    {
        var current = new SlotKey(existing.Date, existing.StartTime, existing.ServiceId);

        if (current != origin)
        {
            throw new DomainRuleException(PersistenceErrors.ConcurrencyCode, PersistenceErrors.AppointmentModifiedMessage);
        }
    }

    // Existence and deletion checks (LINQ, AsNoTracking). Null when the row does not exist; otherwise true when soft-deleted.
    private Task<bool?> PetDeletedFlagAsync(Guid petId, CancellationToken cancellationToken) =>
        context.Pets
            .AsNoTracking()
            .Where(pet => pet.Id == petId)
            .Select(pet => (bool?)(pet.DeletedAt != null))
            .FirstOrDefaultAsync(cancellationToken);

    private Task<bool?> ServiceDeletedFlagAsync(Guid serviceId, CancellationToken cancellationToken) =>
        context.Services
            .AsNoTracking()
            .Where(service => service.Id == serviceId)
            .Select(service => (bool?)(service.DeletedAt != null))
            .FirstOrDefaultAsync(cancellationToken);

    private Task<bool?> OwnerDeletedFlagAsync(Guid ownerId, CancellationToken cancellationToken) =>
        context.Users
            .AsNoTracking()
            .Where(user => user.Id == ownerId)
            .Select(user => (bool?)(user.DeletedAt != null))
            .FirstOrDefaultAsync(cancellationToken);

    // Confirmed appointments of the target slot plus the pet's confirmed appointments, without duplicates.
    // The appointment being rescheduled is excluded by SchedulingRules (excludeAppointmentId), so it is not counted against itself.
    private async Task<List<AppointmentSummary>> LoadRelevantAsync(
        DateOnly date,
        TimeSlot startTime,
        Guid serviceId,
        Guid petId,
        CancellationToken cancellationToken)
    {
        var slot = await appointments.GetSlotSummaryAsync(date, startTime, serviceId, cancellationToken);
        var pet = await appointments.GetPetSummaryAsync(petId, cancellationToken);

        return slot.Concat(pet).DistinctBy(summary => summary.Id).ToList();
    }

    private readonly record struct SlotKey(DateOnly Date, TimeSlot StartTime, Guid ServiceId);

    private readonly record struct AppointmentSnapshot(SlotKey Slot, int Version, AppointmentStatus Status);
}
