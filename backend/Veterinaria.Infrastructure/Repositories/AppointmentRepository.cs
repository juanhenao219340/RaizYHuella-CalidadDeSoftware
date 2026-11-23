using Microsoft.EntityFrameworkCore;
using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;
using Veterinaria.Domain.ValueObjects;
using Veterinaria.Infrastructure.Persistence;

namespace Veterinaria.Infrastructure.Repositories;

public sealed class AppointmentRepository(VeterinaryDbContext context) : IAppointmentRepository
{

    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Appointments.FirstOrDefaultAsync(appointment => appointment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AppointmentSummary>> GetSlotSummaryAsync(
        DateOnly date,
        TimeSlot startTime,
        Guid serviceId,
        CancellationToken cancellationToken) =>
        await context.Appointments
            .AsNoTracking()
            .Where(appointment => appointment.Date == date
                && appointment.StartTime == startTime
                && appointment.ServiceId == serviceId
                && appointment.Status == AppointmentStatus.Confirmed)
            .Select(appointment => new AppointmentSummary(
                appointment.Id,
                appointment.PetId,
                appointment.Date,
                appointment.StartTime,
                appointment.ServiceId,
                appointment.Status))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AppointmentSummary>> GetPetSummaryAsync(Guid petId, CancellationToken cancellationToken) =>
        await context.Appointments
            .AsNoTracking()
            .Where(appointment => appointment.PetId == petId && appointment.Status == AppointmentStatus.Confirmed)
            .Select(appointment => new AppointmentSummary(
                appointment.Id,
                appointment.PetId,
                appointment.Date,
                appointment.StartTime,
                appointment.ServiceId,
                appointment.Status))
            .ToListAsync(cancellationToken);


    public async Task<IReadOnlyDictionary<TimeSlot, int>> CountConfirmedByDayAndServiceAsync(
        DateOnly date,
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        var rows = await context.Appointments
            .AsNoTracking()
            .Where(appointment => appointment.Date == date
                && appointment.ServiceId == serviceId
                && appointment.Status == AppointmentStatus.Confirmed)
            .GroupBy(appointment => appointment.StartTime)
            .Select(group => new { StartTime = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.StartTime, row => row.Count);
    }

    public Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        if (context.Entry(appointment).State == EntityState.Detached)
        {
            throw new InvalidOperationException("Appointment must be loaded through this repository before it is updated.");
        }

        return PersistenceErrors.SaveChangesAsync(context, cancellationToken);
    }
}
