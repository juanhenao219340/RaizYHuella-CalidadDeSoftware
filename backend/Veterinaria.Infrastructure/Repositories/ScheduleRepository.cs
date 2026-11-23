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



public sealed class ScheduleRepository(
    VeterinaryDbContext context,
    IAppointmentRepository appointments,
    ReferenceNumberSequenceCounter sequenceCounter) : IScheduleRepository
{

    private const int MaxDiscoveryRetries = 3;

    public async Task<Appointment> BookAsync(
        AppointmentRequest request,
        Guid ownerId,
        DateTimeOffset nowUtc,
        BusinessRulesOptions options,
        CancellationToken cancellationToken)
    {

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



        if (!await LockServiceAsync(request.ServiceId, cancellationToken))
        {
            throw new DomainRuleException("RF-REG-10", "El servicio está eliminado y no puede usarse para nuevas citas.");
        }


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

        var origin = await FindAppointmentSnapshotAsync(appointmentId, cancellationToken);

        if (origin is null)
        {
            return null;
        }



        if (origin.Value.Status == AppointmentStatus.Cancelled)
        {
            throw new DomainRuleException(PersistenceErrors.AppointmentCancelledCode, PersistenceErrors.AppointmentCancelledMessage);
        }


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


        await TakeSlotsAsync([origin.Value.Slot, destination], cancellationToken);


        var existing = await LoadFreshAsync(appointmentId, cancellationToken);

        if (existing is null)
        {
            return null;
        }

        EnsureSameVersion(existing, origin.Value);
        EnsureSameSlot(existing, origin.Value.Slot);

        var relevant = await LoadRelevantAsync(request.Date, request.StartTime, request.ServiceId, existing.PetId, cancellationToken);

        var updated = new AppointmentRequest(existing.PetId, request.Date, request.StartTime, request.ServiceId);


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



    public async Task<bool> DeleteServiceAsync(
        Guid serviceId,
        UserRole actorRole,
        BusinessRulesOptions options,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var serviceExists = await context.Services
            .AsNoTracking()
            .AnyAsync(candidate => candidate.Id == serviceId && candidate.DeletedAt == null, cancellationToken);

        if (!serviceExists)
        {
            return false;
        }


        var captured = await FindFutureConfirmedAsync(serviceId, options, nowUtc, cancellationToken);

        foreach (var slot in captured.Values.Select(snapshot => snapshot.Slot).Distinct())
        {
            await EnsureSlotControlAsync(slot, cancellationToken);
        }

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);



        if (!await LockServiceAsync(serviceId, cancellationToken))
        {
            return false;
        }

        var retries = 0;

        while (true)
        {

            await TakeSlotsAsync(captured.Values.Select(snapshot => snapshot.Slot), cancellationToken);

            var current = await FindFutureConfirmedAsync(serviceId, options, nowUtc, cancellationToken);

            EnsureUnchanged(captured, current);

            var discovered = current.Where(pair => !captured.ContainsKey(pair.Key)).ToList();

            if (discovered.Count == 0)
            {
                break;
            }

            if (retries == MaxDiscoveryRetries)
            {
                throw new DomainRuleException(PersistenceErrors.ConcurrencyCode, PersistenceErrors.AppointmentModifiedMessage);
            }

            retries++;

            foreach (var (appointmentId, snapshot) in discovered)
            {

                if (!await SlotControlExistsAsync(snapshot.Slot, cancellationToken))
                {
                    throw new DomainRuleException(PersistenceErrors.ConcurrencyCode, PersistenceErrors.AppointmentModifiedMessage);
                }

                captured.Add(appointmentId, snapshot);
            }
        }

        var service = await context.Services.FirstOrDefaultAsync(candidate => candidate.Id == serviceId && candidate.DeletedAt == null, cancellationToken);

        if (service is null)
        {
            return false;
        }


        var tracked = await context.Appointments
            .Where(candidate => captured.Keys.Contains(candidate.Id))
            .ToListAsync(cancellationToken);

        if (tracked.Count != captured.Count)
        {
            throw new DomainRuleException(PersistenceErrors.ConcurrencyCode, PersistenceErrors.AppointmentModifiedMessage);
        }

        foreach (var appointment in tracked)
        {
            var expected = captured[appointment.Id];

            EnsureSameVersion(appointment, expected);
            EnsureSameSlot(appointment, expected.Slot);
            appointment.Cancel(actorRole, AppointmentModificationRules.ServiceDeletedReason, nowUtc);
        }

        service.Delete(nowUtc);

        await PersistenceErrors.SaveChangesAsync(context, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }


    private async Task<Dictionary<Guid, AppointmentSnapshot>> FindFutureConfirmedAsync(
        Guid serviceId,
        BusinessRulesOptions options,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var rows = await context.Appointments
            .AsNoTracking()
            .Where(candidate => candidate.ServiceId == serviceId && candidate.Status == AppointmentStatus.Confirmed)
            .ToListAsync(cancellationToken);

        return rows
            .Where(candidate => AppointmentModificationRules.IsFutureConfirmed(candidate, nowUtc, options))
            .ToDictionary(
                candidate => candidate.Id,
                candidate => new AppointmentSnapshot(
                    new SlotKey(candidate.Date, candidate.StartTime, candidate.ServiceId),
                    candidate.Version,
                    candidate.Status));
    }


    private static void EnsureUnchanged(
        Dictionary<Guid, AppointmentSnapshot> captured,
        Dictionary<Guid, AppointmentSnapshot> current)
    {
        foreach (var (appointmentId, snapshot) in captured)
        {
            if (!current.TryGetValue(appointmentId, out var latest) || latest != snapshot)
            {
                throw new DomainRuleException(PersistenceErrors.ConcurrencyCode, PersistenceErrors.AppointmentModifiedMessage);
            }
        }
    }


    private Task<Appointment?> LoadFreshAsync(Guid appointmentId, CancellationToken cancellationToken) =>
        context.Appointments.FirstOrDefaultAsync(appointment => appointment.Id == appointmentId, cancellationToken);



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




    private async Task<bool> LockServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var locked = await context.Services
            .Where(service => service.Id == serviceId && service.DeletedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(service => service.DeletedAt, service => service.DeletedAt), cancellationToken);

        return locked == 1;
    }


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


    private static void EnsureSameVersion(Appointment existing, AppointmentSnapshot captured)
    {
        if (existing.Version != captured.Version || existing.Status != captured.Status)
        {
            throw new DomainRuleException(PersistenceErrors.ConcurrencyCode, PersistenceErrors.AppointmentModifiedMessage);
        }
    }


    private static void EnsureSameSlot(Appointment existing, SlotKey origin)
    {
        var current = new SlotKey(existing.Date, existing.StartTime, existing.ServiceId);

        if (current != origin)
        {
            throw new DomainRuleException(PersistenceErrors.ConcurrencyCode, PersistenceErrors.AppointmentModifiedMessage);
        }
    }


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
