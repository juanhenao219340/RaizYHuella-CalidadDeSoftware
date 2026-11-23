using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Rules;
using Veterinaria.Domain.ValueObjects;

namespace Veterinaria.Application.Abstractions;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AppointmentSummary>> GetSlotSummaryAsync(
        DateOnly date,
        TimeSlot startTime,
        Guid serviceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AppointmentSummary>> GetPetSummaryAsync(Guid petId, CancellationToken cancellationToken);


    Task<IReadOnlyDictionary<TimeSlot, int>> CountConfirmedByDayAndServiceAsync(
        DateOnly date,
        Guid serviceId,
        CancellationToken cancellationToken);

    Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken);
}
