using Veterinaria.Application.Abstractions;

namespace Veterinaria.Application.Pets;

public sealed record DeletePetCommand(Guid PetId, Guid CallerId);

public sealed class DeletePetHandler(IPetRepository petRepository, TimeProvider timeProvider)
{

    public async Task<bool> HandleAsync(DeletePetCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var pet = await petRepository.GetByIdAsync(command.PetId, cancellationToken);

        if (pet is null || pet.OwnerId != command.CallerId)
        {
            return false;
        }


        pet.Delete(timeProvider.GetUtcNow());
        await petRepository.UpdateAsync(pet, cancellationToken);

        return true;
    }
}
