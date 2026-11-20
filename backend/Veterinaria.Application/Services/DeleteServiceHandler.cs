using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Configuration;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Services;

public sealed record DeleteServiceCommand(Guid ServiceId, UserRole ActorRole);

// Administrator only (policy). Cancels the confirmed future appointments of the service and deletes it in one transaction (R18-1, D3).
public sealed class DeleteServiceHandler(IScheduleRepository scheduleRepository, BusinessRulesOptions options, TimeProvider timeProvider)
{
    // Returns false when the service is missing or already deleted (the API answers 404).
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
