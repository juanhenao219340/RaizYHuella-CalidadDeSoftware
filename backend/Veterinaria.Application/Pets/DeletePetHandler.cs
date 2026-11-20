using Veterinaria.Application.Abstractions;

namespace Veterinaria.Application.Pets;

public sealed record DeletePetCommand(Guid PetId, Guid CallerId);

public sealed class DeletePetHandler(IPetRepository petRepository, TimeProvider timeProvider)
{
    // Returns false when the pet is missing, deleted, or not owned by the caller (the API answers 404).
    public async Task<bool> HandleAsync(DeletePetCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var pet = await petRepository.GetByIdAsync(command.PetId, cancellationToken);

        if (pet is null || pet.OwnerId != command.CallerId)
        {
            return false;
        }

        // Pets have no identifier to replace on deletion (RF-REG-10(e)).
        pet.Delete(timeProvider.GetUtcNow());
        await petRepository.UpdateAsync(pet, cancellationToken);

        return true;
    }
}
