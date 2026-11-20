using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Contracts;

// OwnerId is never accepted from the body: it always comes from the JWT subject (RF-PRO-09, D-1).
public sealed record CreatePetRequest
{
    public const int MaxIdentifierLength = 50;
    public const int MaxNameLength = 100;
    public const int MaxSpeciesLength = 50;
    public const int MaxBreedLength = 100;

    [Required(ErrorMessage = "El identificador de la mascota es obligatorio.")]
    [StringLength(MaxIdentifierLength, ErrorMessage = "El identificador no puede superar 50 caracteres.")]
    [NotReservedPrefix]
    public string Identifier { get; init; } = string.Empty;

    [Required(ErrorMessage = "El nombre de la mascota es obligatorio.")]
    [StringLength(MaxNameLength, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "La especie es obligatoria.")]
    [StringLength(MaxSpeciesLength, ErrorMessage = "La especie no puede superar 50 caracteres.")]
    public string Species { get; init; } = string.Empty;

    [Required(ErrorMessage = "La raza es obligatoria.")]
    [StringLength(MaxBreedLength, ErrorMessage = "La raza no puede superar 100 caracteres.")]
    public string Breed { get; init; } = string.Empty;

    // Nullable so that a missing value yields a Spanish 400 instead of the default date (D-8).
    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateOnly? BirthDate { get; init; }
}

// Identifier and owner are not editable (D-6, RF-PRO-09). Any such field sent by the client is ignored.
public sealed record UpdatePetRequest
{
    [Required(ErrorMessage = "El nombre de la mascota es obligatorio.")]
    [StringLength(CreatePetRequest.MaxNameLength, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "La especie es obligatoria.")]
    [StringLength(CreatePetRequest.MaxSpeciesLength, ErrorMessage = "La especie no puede superar 50 caracteres.")]
    public string Species { get; init; } = string.Empty;

    [Required(ErrorMessage = "La raza es obligatoria.")]
    [StringLength(CreatePetRequest.MaxBreedLength, ErrorMessage = "La raza no puede superar 100 caracteres.")]
    public string Breed { get; init; } = string.Empty;

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateOnly? BirthDate { get; init; }
}

// OwnerName and OwnerDeleted are filled only for administrators (RF-REG-07 a, R14-1); owners receive null for both.
// Nullable bool so that the owner response keeps the same keys with null values (D-2).
public sealed record PetResponse(
    Guid Id,
    string Identifier,
    string Name,
    string Species,
    string Breed,
    DateOnly BirthDate,
    Guid OwnerId,
    string? OwnerName,
    bool? OwnerDeleted);
