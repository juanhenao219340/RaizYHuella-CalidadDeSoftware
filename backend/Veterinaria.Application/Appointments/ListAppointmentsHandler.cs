using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Appointments;

// Filters are already trimmed and validated by the API. CallerId and CallerRole come from the JWT only (RF-PRO-09).
public sealed record ListAppointmentsQuery(
    Guid CallerId,
    UserRole CallerRole,
    string? ReferenceNumber,
    string? PetIdentifier,
    string? OwnerIdentification,
    ServiceType? ServiceType);

public sealed class ListAppointmentsHandler(IAppointmentSearchRepository searchRepository)
{
    public async Task<IReadOnlyList<AppointmentListItem>> HandleAsync(ListAppointmentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Fail closed: an unknown role never reaches the repository (RF-CON-05, RNF-SEG-03).
        if (query.CallerRole is not (UserRole.Administrator or UserRole.Owner))
        {
            return [];
        }

        // Administrators: no scope. Owners: only their own pets' appointments, applied first in the query (RF-CON-05, RF-CON-07).
        var ownerId = query.CallerRole == UserRole.Owner ? query.CallerId : (Guid?)null;

        var criteria = new AppointmentSearchCriteria(
            query.ReferenceNumber,
            query.PetIdentifier,
            query.OwnerIdentification,
            query.ServiceType,
            ownerId);

        return await searchRepository.SearchAsync(criteria, cancellationToken);
    }
}
