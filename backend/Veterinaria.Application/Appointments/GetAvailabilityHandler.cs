using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Configuration;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;
using Veterinaria.Domain.ValueObjects;

namespace Veterinaria.Application.Appointments;

public sealed record GetAvailabilityQuery(Guid ServiceId, DateOnly Date, UserRole CallerRole);

public sealed record SlotAvailability(TimeSlot StartTime, int RemainingSlots, bool Available);


public sealed class GetAvailabilityHandler(
    IServiceRepository serviceRepository,
    IAppointmentRepository appointmentRepository,
    BusinessRulesOptions options,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<SlotAvailability>?> HandleAsync(GetAvailabilityQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);


        if (await serviceRepository.GetByIdAsync(query.ServiceId, cancellationToken) is null)
        {
            return null;
        }

        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), options.GetTimeZone()).DateTime;
        var today = DateOnly.FromDateTime(localNow);
        var currentTime = TimeOnly.FromDateTime(localNow);

        if (query.Date < today)
        {
            throw new DomainRuleException("RN-01", "La fecha consultada es anterior a la fecha actual del sistema.");
        }

        var confirmed = await appointmentRepository.CountConfirmedByDayAndServiceAsync(query.Date, query.ServiceId, cancellationToken);

        var result = new List<SlotAvailability>();

        for (var hour = TimeSlot.FirstSlotHour; hour <= TimeSlot.LastSlotHour; hour++)
        {
            var slot = TimeSlot.Create(new TimeOnly(hour, 0));
            var remaining = SlotCapacityCalculator.Slots(options.SlotCapacity, confirmed.GetValueOrDefault(slot));


            var notPassed = query.Date > today || slot.StartTime > currentTime;

            result.Add(new SlotAvailability(slot, remaining, SlotCapacityCalculator.IsAvailable(remaining) && notPassed));
        }


        if (query.CallerRole == UserRole.Administrator)
        {
            result = result.Where(slot => slot.Available).ToList();
        }

        return result;
    }
}
