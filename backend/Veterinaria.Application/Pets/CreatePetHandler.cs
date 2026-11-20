using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Rules;

namespace Veterinaria.Application.Pets;

public sealed record CreatePetCommand(
    string Identifier,
    string Name,
    string Species,
    string Breed,
    DateOnly BirthDate,
    Guid OwnerId);

public sealed class CreatePetHandler(IPetRepository petRepository)
{
    public async Task<Pet> HandleAsync(CreatePetCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await petRepository.IdentifierExistsAsync(command.Identifier, cancellationToken))
        {
            throw new DomainRuleException("RF-REG-06", "El identificador de la mascota ya está registrado.");
        }

        var pet = new Pet(
            Guid.NewGuid(),
            command.Identifier,
            command.Name,
            command.Species,
            command.Breed,
            command.BirthDate,
            command.OwnerId);

        await petRepository.CreateAsync(pet, cancellationToken);

        return pet;
    }
}
