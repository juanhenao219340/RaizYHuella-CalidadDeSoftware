using System.Globalization;
using Veterinaria.Domain.Rules;

namespace Veterinaria.Domain.ValueObjects;

public readonly record struct TimeSlot
{
    public const int FirstSlotHour = 8;
    public const int LastSlotHour = 17;

    private TimeSlot(TimeOnly startTime) => StartTime = startTime;

    public TimeOnly StartTime { get; }

    public static TimeSlot Create(TimeOnly startTime)
    {
        var onTheHour = startTime == new TimeOnly(startTime.Hour, 0);
        var withinRange = startTime.Hour >= FirstSlotHour && startTime.Hour <= LastSlotHour;

        if (!onTheHour || !withinRange)
        {
            throw new DomainRuleException("RF-PRO-01", "La hora de inicio debe estar entre 08:00 y 17:00, en punto.");
        }

        return new TimeSlot(startTime);
    }

    public override string ToString() => StartTime.ToString("HH:mm", CultureInfo.InvariantCulture);
}
