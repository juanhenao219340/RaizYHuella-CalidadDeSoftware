using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Services;

public sealed record CreateServiceCommand(string Name, ServiceType Type);

public sealed class CreateServiceHandler(IServiceRepository serviceRepository)
{
    public async Task<Service> HandleAsync(CreateServiceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var service = new Service(Guid.NewGuid(), command.Name, command.Type);

        await serviceRepository.CreateAsync(service, cancellationToken);

        return service;
    }
}
