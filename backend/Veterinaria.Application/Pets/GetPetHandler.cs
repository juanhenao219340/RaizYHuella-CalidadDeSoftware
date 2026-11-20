using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Pets;

public sealed record GetPetQuery(Guid PetId, Guid CallerId, UserRole CallerRole);

public sealed class GetPetHandler(IPetRepository petRepository)
{
    // Owners see only their own pets; administrators see every non-deleted pet (read-only, OD-4).
    // Administrators also get the owner name and deletion state (R14-1); owners get no owner data (null).
    public async Task<PetWithOwner?> HandleAsync(GetPetQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CallerRole == UserRole.Administrator)
        {
            return await petRepository.GetByIdWithOwnerAsync(query.PetId, cancellationToken);
        }

        var pet = await petRepository.GetByIdAsync(query.PetId, cancellationToken);

        if (pet is null)
        {
            return null;
        }

        if (query.CallerRole == UserRole.Owner && pet.OwnerId != query.CallerId)
        {
            return null;
        }

        return new PetWithOwner(pet, OwnerName: null, OwnerDeleted: null);
    }
}
