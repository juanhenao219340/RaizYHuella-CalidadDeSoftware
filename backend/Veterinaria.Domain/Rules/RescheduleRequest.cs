using Veterinaria.Domain.ValueObjects;

namespace Veterinaria.Domain.Rules;

// New slot and service of an administrative reschedule. The pet is not part of the request: it is kept from the appointment.
public sealed record RescheduleRequest(
    DateOnly Date,
    TimeSlot StartTime,
    Guid ServiceId);
