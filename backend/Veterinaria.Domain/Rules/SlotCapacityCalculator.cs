using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.ValueObjects;

namespace Veterinaria.Domain.Rules;

public static class SlotCapacityCalculator
{
    public static int Slots(int capacity, int confirmed)
    {
        if (capacity <= 0 || confirmed < 0)
        {
            throw new DomainRuleException("RN-02", "La capacidad por franja debe ser mayor que cero y la ocupación no puede ser negativa.");
        }

        return Math.Max(0, capacity - confirmed);
    }

    public static bool IsAvailable(int slots) => slots > 0;

    public static int CountConfirmed(
        IEnumerable<AppointmentSummary> appointments,
        DateOnly date,
        TimeSlot startTime,
        Guid serviceId,
        Guid? excludeAppointmentId = null) =>
        appointments.Count(c =>
            c.Id != excludeAppointmentId
            && c.Status == AppointmentStatus.Confirmed
            && c.Date == date
            && c.StartTime == startTime
            && c.ServiceId == serviceId);
}
