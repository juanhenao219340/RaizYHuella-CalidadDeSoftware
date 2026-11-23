using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;

namespace Veterinaria.Application.Pets;


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
