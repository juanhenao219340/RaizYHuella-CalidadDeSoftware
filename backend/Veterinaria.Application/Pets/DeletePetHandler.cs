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

        // The identifier is replaced so the original value can be reused (RF-REG-10(e)).
        pet.Delete(Guid.NewGuid(), timeProvider.GetUtcNow());
        await petRepository.UpdateAsync(pet, cancellationToken);

        return true;
    }
}
