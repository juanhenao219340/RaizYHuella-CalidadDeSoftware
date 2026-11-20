using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Pets;

public sealed record ListPetsQuery(Guid CallerId, UserRole CallerRole);

public sealed class ListPetsHandler(IPetRepository petRepository)
{
    // Administrators also get the owner name and deletion state (R14-1); owners get no owner data (null).
    public async Task<IReadOnlyList<PetWithOwner>> HandleAsync(ListPetsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CallerRole == UserRole.Administrator)
        {
            return await petRepository.ListAllWithOwnerAsync(cancellationToken);
        }

        var pets = await petRepository.ListByOwnerAsync(query.CallerId, cancellationToken);

        return pets.Select(pet => new PetWithOwner(pet, OwnerName: null, OwnerDeleted: null)).ToList();
    }
}
