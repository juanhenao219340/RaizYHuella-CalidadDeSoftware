using Veterinaria.Application.Appointments;

namespace Veterinaria.Application.Abstractions;

// Read-only search for the appointment list (T-07). Separate from IAppointmentRepository, which stays write- and availability-only.
public interface IAppointmentSearchRepository
{
    Task<IReadOnlyList<AppointmentListItem>> SearchAsync(AppointmentSearchCriteria criteria, CancellationToken cancellationToken);
}
