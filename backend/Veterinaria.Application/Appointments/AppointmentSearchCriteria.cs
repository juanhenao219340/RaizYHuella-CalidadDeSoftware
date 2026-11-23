using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Appointments;



public sealed record AppointmentSearchCriteria(
    string? ReferenceNumber,
    string? PetName,
    string? OwnerIdentification,
    ServiceType? ServiceType,
    Guid? OwnerId);
