using Microsoft.EntityFrameworkCore;
using Veterinaria.Application.Abstractions;
using Veterinaria.Application.Pets;
using Veterinaria.Domain.Entities;
using Veterinaria.Infrastructure.Persistence;

namespace Veterinaria.Infrastructure.Repositories;

public sealed class PetRepository(VeterinaryDbContext context) : IPetRepository
{
    public Task<Pet?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Pets.FirstOrDefaultAsync(pet => pet.Id == id && pet.DeletedAt == null, cancellationToken);

    public Task<Pet?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken) =>
        context.Pets.AsNoTracking().FirstOrDefaultAsync(pet => pet.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Pet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await context.Pets
            .AsNoTracking()
            .Where(pet => pet.OwnerId == ownerId && pet.DeletedAt == null)
            .OrderBy(pet => pet.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Pet>> ListAllAsync(CancellationToken cancellationToken) =>
        await context.Pets
            .AsNoTracking()
            .Where(pet => pet.DeletedAt == null)
            .OrderBy(pet => pet.Name)
            .ToListAsync(cancellationToken);

    // One query per call, no per-row lookups (RNF-ARQ-07: LINQ only, no hand-written SQL).
    public async Task<IReadOnlyList<PetWithOwner>> ListAllWithOwnerAsync(CancellationToken cancellationToken) =>
        await WithOwner(context.Pets
                .AsNoTracking()
                .Where(pet => pet.DeletedAt == null)
                .OrderBy(pet => pet.Name))
            .ToListAsync(cancellationToken);

    public Task<PetWithOwner?> GetByIdWithOwnerAsync(Guid id, CancellationToken cancellationToken) =>
        WithOwner(context.Pets
                .AsNoTracking()
                .Where(pet => pet.Id == id && pet.DeletedAt == null))
            .FirstOrDefaultAsync(cancellationToken);

    // LEFT JOIN to the users table. Deleted owners are NOT filtered out (FB-2, RF-REG-10 f).
    // A pet whose owner row is missing is kept, with null owner data (D-3).
    private IQueryable<PetWithOwner> WithOwner(IQueryable<Pet> pets) =>
        pets
            .GroupJoin(context.Users, pet => pet.OwnerId, user => user.Id, (pet, owners) => new { pet, owners })
            .SelectMany(
                row => row.owners.DefaultIfEmpty(),
                (row, owner) => new PetWithOwner(
                    row.pet,
                    owner == null ? null : owner.Name,
                    owner == null ? null : owner.DeletedAt != null));

    public async Task CreateAsync(Pet pet, CancellationToken cancellationToken)
    {
        context.Pets.Add(pet);
        await PersistenceErrors.SaveChangesAsync(context, cancellationToken);
    }

    public async Task UpdateAsync(Pet pet, CancellationToken cancellationToken)
    {
        if (context.Entry(pet).State == EntityState.Detached)
        {
            context.Pets.Update(pet);
        }

        await PersistenceErrors.SaveChangesAsync(context, cancellationToken);
    }
}
