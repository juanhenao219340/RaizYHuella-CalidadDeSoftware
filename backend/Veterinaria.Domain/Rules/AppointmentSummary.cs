using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.ValueObjects;

namespace Veterinaria.Domain.Rules;

public sealed record AppointmentSummary(
    Guid Id,
    Guid PetId,
    DateOnly Date,
    TimeSlot StartTime,
    Guid ServiceId,
    AppointmentStatus Status);
