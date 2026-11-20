using Veterinaria.Domain.Configuration;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Domain.Rules;

public static class SchedulingRules
{
    public static DomainResult Validate(
        AppointmentRequest request,
        DateTimeOffset nowUtc,
        BusinessRulesOptions options,
        IReadOnlyCollection<AppointmentSummary> appointments,
        Guid? excludeAppointmentId)
    {
        var localNow = TimeZoneInfo.ConvertTime(nowUtc, options.GetTimeZone()).DateTime;
        var today = DateOnly.FromDateTime(localNow);
        var currentTime = TimeOnly.FromDateTime(localNow);

        if (request.Date < today)
        {
            return DomainResult.Failed("RN-01", "La fecha de la cita no puede ser anterior a la fecha actual del sistema.");
        }

        if (request.Date == today && request.StartTime.StartTime <= currentTime)
        {
            return DomainResult.Failed("P-01", "Para el día actual, la hora de inicio debe ser posterior a la hora actual del sistema.");
        }

        var petAlreadyHasAppointment = appointments.Any(c =>
            c.Id != excludeAppointmentId
            && c.PetId == request.PetId
            && c.Status != AppointmentStatus.Cancelled
            && c.Date == request.Date
            && c.StartTime == request.StartTime
            && c.ServiceId == request.ServiceId);

        if (petAlreadyHasAppointment)
        {
            return DomainResult.Failed("P-02", "La mascota ya tiene una cita activa en esa fecha, hora y servicio.");
        }

        if (options.SlotCapacity <= 0)
        {
            return DomainResult.Failed("RN-02", "La capacidad por franja debe ser mayor que cero y la ocupación no puede ser negativa.");
        }

        var confirmed = SlotCapacityCalculator.CountConfirmed(appointments, request.Date, request.StartTime, request.ServiceId, excludeAppointmentId);
        var slots = SlotCapacityCalculator.Slots(options.SlotCapacity, confirmed);

        if (!SlotCapacityCalculator.IsAvailable(slots))
        {
            return DomainResult.Failed("RN-03", "La franja no tiene cupos disponibles.");
        }

        return DomainResult.Success();
    }
}
