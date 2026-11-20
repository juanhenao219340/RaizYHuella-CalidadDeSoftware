using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Services;

public sealed record UpdateServiceCommand(Guid ServiceId, string Name, ServiceType Type);

public sealed class UpdateServiceHandler(IServiceRepository serviceRepository)
{
    public async Task<Service?> HandleAsync(UpdateServiceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var service = await serviceRepository.GetByIdAsync(command.ServiceId, cancellationToken);

        if (service is null)
        {
            return null;
        }

        service.Update(command.Name, command.Type);
        await serviceRepository.UpdateAsync(service, cancellationToken);

        return service;
    }
}
