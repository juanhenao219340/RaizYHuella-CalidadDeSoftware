using System.Globalization;
using Veterinaria.Domain.Rules;
using Veterinaria.Domain.ValueObjects;

namespace Veterinaria.Application.Appointments;

public static class AppointmentStartTime
{
    public static TimeSlot Parse(string startTime) =>
        TimeOnly.TryParseExact(startTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? TimeSlot.Create(time)
            : throw new DomainRuleException("RF-PRO-01", "La hora de inicio debe estar entre 08:00 y 17:00, en punto.");
}
