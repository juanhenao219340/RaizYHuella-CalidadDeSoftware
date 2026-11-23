using Veterinaria.Application.Appointments;

namespace Veterinaria.Application.Abstractions;


public interface IAppointmentSearchRepository
{
    Task<IReadOnlyList<AppointmentListItem>> SearchAsync(AppointmentSearchCriteria criteria, CancellationToken cancellationToken);
}
