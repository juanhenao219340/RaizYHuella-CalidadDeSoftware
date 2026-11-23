using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Appointments;


public sealed record ListAppointmentsQuery(
    Guid CallerId,
    UserRole CallerRole,
    string? ReferenceNumber,
    string? PetName,
    string? OwnerIdentification,
    ServiceType? ServiceType);

public sealed class ListAppointmentsHandler(IAppointmentSearchRepository searchRepository)
{
    public async Task<IReadOnlyList<AppointmentListItem>> HandleAsync(ListAppointmentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);


        if (query.CallerRole is not (UserRole.Administrator or UserRole.Owner))
        {
            return [];
        }


        var ownerId = query.CallerRole == UserRole.Owner ? query.CallerId : (Guid?)null;

        var criteria = new AppointmentSearchCriteria(
            query.ReferenceNumber,
            query.PetName,
            query.OwnerIdentification,
            query.ServiceType,
            ownerId);

        return await searchRepository.SearchAsync(criteria, cancellationToken);
    }
}
