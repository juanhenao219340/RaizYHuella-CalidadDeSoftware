using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Contracts;


public sealed record CreatePetRequest
{
    public const int MaxNameLength = 100;
    public const int MaxSpeciesLength = 50;
    public const int MaxBreedLength = 100;

    [Required(ErrorMessage = "El nombre de la mascota es obligatorio.")]
    [StringLength(MaxNameLength, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "La especie es obligatoria.")]
    [StringLength(MaxSpeciesLength, ErrorMessage = "La especie no puede superar 50 caracteres.")]
    public string Species { get; init; } = string.Empty;

    [Required(ErrorMessage = "La raza es obligatoria.")]
    [StringLength(MaxBreedLength, ErrorMessage = "La raza no puede superar 100 caracteres.")]
    public string Breed { get; init; } = string.Empty;


    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateOnly? BirthDate { get; init; }
}


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



public sealed record PetResponse(
    Guid Id,
    string Name,
    string Species,
    string Breed,
    DateOnly BirthDate,
    Guid OwnerId,
    string? OwnerName,
    bool? OwnerDeleted);
