using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Configuration;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;

namespace Veterinaria.Application.Appointments;

public sealed record RescheduleAppointmentCommand(
    Guid AppointmentId,
    UserRole ActorRole,
    Guid ServiceId,
    DateOnly Date,
    string StartTime,
    string? Reason);


public sealed class RescheduleAppointmentHandler(
    IScheduleRepository scheduleRepository,
    BusinessRulesOptions options,
    TimeProvider timeProvider)
{
    public async Task<Appointment?> HandleAsync(RescheduleAppointmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var startTime = AppointmentStartTime.Parse(command.StartTime);
        var request = new RescheduleRequest(command.Date, startTime, command.ServiceId);

        return await scheduleRepository.RescheduleAsync(
            command.AppointmentId,
            request,
            command.ActorRole,
            command.Reason,
            timeProvider.GetUtcNow(),
            options,
            cancellationToken);
    }
}
