using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Configuration;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Services;

public sealed record DeleteServiceCommand(Guid ServiceId, UserRole ActorRole);


public sealed class DeleteServiceHandler(IScheduleRepository scheduleRepository, BusinessRulesOptions options, TimeProvider timeProvider)
{

    public async Task<bool> HandleAsync(DeleteServiceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await scheduleRepository.DeleteServiceAsync(
            command.ServiceId,
            command.ActorRole,
            options,
            timeProvider.GetUtcNow(),
            cancellationToken);
    }
}
