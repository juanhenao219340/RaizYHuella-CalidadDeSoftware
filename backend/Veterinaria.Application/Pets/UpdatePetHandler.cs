using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;

namespace Veterinaria.Application.Pets;

// The identifier is not editable (D-6). Only the owner of the pet can update it; anything else is not visible (404).
public sealed record UpdatePetCommand(
    Guid PetId,
    Guid CallerId,
    string Name,
    string Species,
    string Breed,
    DateOnly BirthDate);

public sealed class UpdatePetHandler(IPetRepository petRepository)
{
    public async Task<Pet?> HandleAsync(UpdatePetCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var pet = await petRepository.GetByIdAsync(command.PetId, cancellationToken);

        if (pet is null || pet.OwnerId != command.CallerId)
        {
            return null;
        }

        pet.Update(command.Name, command.Species, command.Breed, command.BirthDate);
        await petRepository.UpdateAsync(pet, cancellationToken);

        return pet;
    }
}
