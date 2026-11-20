using Veterinaria.Domain.Rules;

namespace Veterinaria.Domain.Entities;

public class Pet
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Species { get; private set; } = string.Empty;

    public string Breed { get; private set; } = string.Empty;

    public DateOnly BirthDate { get; private set; }

    public Guid OwnerId { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    private Pet()
    {
    }

    public Pet(Guid id, string name, string species, string breed, DateOnly birthDate, Guid ownerId)
    {
        var candidate = new Pet
        {
            Id = id,
            Name = name,
            Species = species,
            Breed = breed,
            BirthDate = birthDate,
            OwnerId = ownerId
        };

        Validate(candidate).ThrowIfFailed();

        Id = candidate.Id;
        Name = candidate.Name;
        Species = candidate.Species;
        Breed = candidate.Breed;
        BirthDate = candidate.BirthDate;
        OwnerId = candidate.OwnerId;
    }

    public void Update(string name, string species, string breed, DateOnly birthDate)
    {
        if (IsDeleted)
        {
            throw new DomainRuleException("RF-REG-10", "La mascota está eliminada y no puede modificarse.");
        }

        var candidate = new Pet
        {
            Id = Id,
            Name = name,
            Species = species,
            Breed = breed,
            BirthDate = birthDate,
            OwnerId = OwnerId
        };

        Validate(candidate).ThrowIfFailed();

        Name = candidate.Name;
        Species = candidate.Species;
        Breed = candidate.Breed;
        BirthDate = candidate.BirthDate;
    }

    // Logical deletion (RF-REG-10). The pet has no identifier to release.
    public void Delete(DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainRuleException("RF-REG-10", "La mascota ya está eliminada.");
        }

        DeletedAt = deletedAt;
    }

    public static DomainResult Validate(Pet pet)
    {
        var emptyFields = string.IsNullOrWhiteSpace(pet.Name)
            || string.IsNullOrWhiteSpace(pet.Species)
            || string.IsNullOrWhiteSpace(pet.Breed);

        if (emptyFields || pet.OwnerId == Guid.Empty)
        {
            return DomainResult.Failed("RF-REG-05", "El nombre, especie, raza y propietario de la mascota son obligatorios.");
        }

        return DomainResult.Success();
    }
}
