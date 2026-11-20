using Veterinaria.Domain.ValueObjects;

namespace Veterinaria.Domain.Entities;

// Control de turno por franja (Date, StartTime, ServiceId). Sin contador de ocupación: el cupo se cuenta sobre citas confirmadas (RN-02).
// Solo serializa reservas, reprogramaciones y anulaciones: el turno se toma con ExecuteUpdateAsync dentro de la transacción.
public class SlotControl
{
    public DateOnly Date { get; private set; }

    public TimeSlot StartTime { get; private set; }

    public Guid ServiceId { get; private set; }

    // Incrementado por ExecuteUpdateAsync al tomar el turno. Se mantiene como columna normal (no token de concurrencia).
    public int Version { get; private set; }

    // Parameterless constructor reserved for EF Core (materialization).
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
