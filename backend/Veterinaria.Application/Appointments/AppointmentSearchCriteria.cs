using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Appointments;

// Filters are already trimmed and validated by the API. A null filter means "not requested".
// OwnerId is null for administrators and holds the JWT subject for owners; it is always applied first in the query.
public sealed record AppointmentSearchCriteria(
    string? ReferenceNumber,
    string? PetIdentifier,
    string? OwnerIdentification,
    ServiceType? ServiceType,
    Guid? OwnerId);
