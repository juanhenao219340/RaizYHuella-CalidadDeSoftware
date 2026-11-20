using Veterinaria.Application.Pets;
using Veterinaria.Domain.Entities;

namespace Veterinaria.Application.Abstractions;

public interface IPetRepository
{
    Task<Pet?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Includes soft-deleted pets; used by appointment creation to answer RF-REG-10 instead of 404 (T-06).
    Task<Pet?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Pet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Pet>> ListAllAsync(CancellationToken cancellationToken);

    // Administrator reads: non-deleted pets with owner name and owner deletion state, in one query (R14-1, RF-REG-07 a).
    Task<IReadOnlyList<PetWithOwner>> ListAllWithOwnerAsync(CancellationToken cancellationToken);

    Task<PetWithOwner?> GetByIdWithOwnerAsync(Guid id, CancellationToken cancellationToken);

    Task CreateAsync(Pet pet, CancellationToken cancellationToken);

    Task UpdateAsync(Pet pet, CancellationToken cancellationToken);
}
