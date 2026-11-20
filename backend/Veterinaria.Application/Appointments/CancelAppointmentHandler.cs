using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Appointments;

public sealed record CancelAppointmentCommand(Guid AppointmentId, UserRole ActorRole, string? Reason);

// Administrator only (policy). Cancellation only releases capacity, so no slot or pet lock is taken.
public sealed class CancelAppointmentHandler(IScheduleRepository scheduleRepository, TimeProvider timeProvider)
{
    public async Task<Appointment?> HandleAsync(CancelAppointmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await scheduleRepository.CancelAsync(
            command.AppointmentId,
            command.ActorRole,
            command.Reason,
            timeProvider.GetUtcNow(),
            cancellationToken);
    }
}
