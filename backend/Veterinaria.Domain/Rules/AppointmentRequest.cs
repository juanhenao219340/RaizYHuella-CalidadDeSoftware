using Veterinaria.Domain.ValueObjects;

namespace Veterinaria.Domain.Rules;

public sealed record AppointmentRequest(
    Guid PetId,
    DateOnly Date,
    TimeSlot StartTime,
    Guid ServiceId);
