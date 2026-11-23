using Veterinaria.Application.Pets;
using Veterinaria.Domain.Entities;

namespace Veterinaria.Application.Abstractions;

public interface IPetRepository
{
    Task<Pet?> GetByIdAsync(Guid id, CancellationToken cancellationToken);


    Task<Pet?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Pet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Pet>> ListAllAsync(CancellationToken cancellationToken);


    Task<IReadOnlyList<PetWithOwner>> ListAllWithOwnerAsync(CancellationToken cancellationToken);

    Task<PetWithOwner?> GetByIdWithOwnerAsync(Guid id, CancellationToken cancellationToken);

    Task CreateAsync(Pet pet, CancellationToken cancellationToken);

    Task UpdateAsync(Pet pet, CancellationToken cancellationToken);
}
