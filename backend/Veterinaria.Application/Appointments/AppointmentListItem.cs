using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Appointments;

// Read model for the appointment list (T-07). Deleted pets and services keep their name and type (RF-REG-10).
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
    AppointmentStatus Status);
