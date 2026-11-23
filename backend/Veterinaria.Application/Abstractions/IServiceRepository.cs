using Veterinaria.Domain.Entities;

namespace Veterinaria.Application.Abstractions;

public interface IServiceRepository
{
    Task<IReadOnlyList<Service>> ListAsync(CancellationToken cancellationToken);

    Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken);


    Task<Service?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken);

    Task CreateAsync(Service service, CancellationToken cancellationToken);

    Task UpdateAsync(Service service, CancellationToken cancellationToken);
}
