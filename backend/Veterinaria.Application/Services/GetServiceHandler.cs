using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;

namespace Veterinaria.Application.Services;

public sealed class GetServiceHandler(IServiceRepository serviceRepository)
{
    public Task<Service?> HandleAsync(Guid serviceId, CancellationToken cancellationToken) =>
        serviceRepository.GetByIdAsync(serviceId, cancellationToken);
}
