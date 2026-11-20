using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;

namespace Veterinaria.Application.Services;

public sealed class ListServicesHandler(IServiceRepository serviceRepository)
{
    public Task<IReadOnlyList<Service>> HandleAsync(CancellationToken cancellationToken) =>
        serviceRepository.ListAsync(cancellationToken);
}
