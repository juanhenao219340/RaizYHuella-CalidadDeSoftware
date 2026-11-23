using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Appointments;


public sealed record AppointmentListItem(
    Guid Id,
    string ReferenceNumber,
    Guid PetId,
    string PetName,
    Guid ServiceId,
    string ServiceName,
    ServiceType ServiceType,
    DateOnly Date,
    string StartTime,
    AppointmentStatus Status,

    string? OwnerName);
