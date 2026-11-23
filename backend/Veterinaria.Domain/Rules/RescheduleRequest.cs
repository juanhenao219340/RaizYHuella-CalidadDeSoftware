using Veterinaria.Domain.ValueObjects;

namespace Veterinaria.Domain.Rules;


public sealed record RescheduleRequest(
    DateOnly Date,
    TimeSlot StartTime,
    Guid ServiceId);
