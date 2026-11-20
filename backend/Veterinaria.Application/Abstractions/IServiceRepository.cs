using Veterinaria.Domain.Entities;

namespace Veterinaria.Application.Abstractions;

public interface IServiceRepository
{
    Task<IReadOnlyList<Service>> ListAsync(CancellationToken cancellationToken);

    Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Includes soft-deleted services; used by appointment creation to answer RF-REG-10 instead of 404 (T-06).
    Task<Service?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken);

    Task CreateAsync(Service service, CancellationToken cancellationToken);

    Task UpdateAsync(Service service, CancellationToken cancellationToken);
}
