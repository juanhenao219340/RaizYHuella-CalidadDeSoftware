using Veterinaria.Domain.ValueObjects;

namespace Veterinaria.Domain.Entities;



public class SlotControl
{
    public DateOnly Date { get; private set; }

    public TimeSlot StartTime { get; private set; }

    public Guid ServiceId { get; private set; }


    public int Version { get; private set; }


    private SlotControl()
    {
    }

    public static SlotControl Create(DateOnly date, TimeSlot startTime, Guid serviceId) =>
        new()
        {
            Date = date,
            StartTime = startTime,
            ServiceId = serviceId,
            Version = 0
        };
}
