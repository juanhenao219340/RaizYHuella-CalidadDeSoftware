using Veterinaria.Domain.Configuration;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;

namespace Veterinaria.Application.Abstractions;

public interface IScheduleRepository
{
    Task<Appointment> BookAsync(
        AppointmentRequest request,
        Guid ownerId,
        DateTimeOffset nowUtc,
        BusinessRulesOptions options,
        CancellationToken cancellationToken);

    Task<Appointment?> RescheduleAsync(
        Guid appointmentId,
        RescheduleRequest request,
        UserRole actorRole,
        string? reason,
        DateTimeOffset nowUtc,
        BusinessRulesOptions options,
        CancellationToken cancellationToken);

    Task<Appointment?> CancelAsync(
        Guid appointmentId,
        UserRole actorRole,
        string? reason,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    // Cancels the confirmed future appointments of the service (R18-1) and deletes it, in one transaction. False when the service does not exist or is already deleted.
    Task<bool> DeleteServiceAsync(
        Guid serviceId,
        UserRole actorRole,
        BusinessRulesOptions options,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);
}
