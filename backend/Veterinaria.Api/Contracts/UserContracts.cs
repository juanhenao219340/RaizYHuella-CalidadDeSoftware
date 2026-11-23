using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Contracts;

public sealed record CreateOwnerRequest
{
    public const int MaxIdentificationLength = 20;
    public const int MaxNameLength = 150;
    public const int MaxEmailLength = 256;
    public const int MaxPhoneLength = 30;

    [Required(ErrorMessage = "La identificación es obligatoria.")]
    [StringLength(MaxIdentificationLength, ErrorMessage = "La identificación no puede superar 20 caracteres.")]
    [NotReservedPrefix]
    public string Identification { get; init; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(MaxNameLength, ErrorMessage = "El nombre no puede superar 150 caracteres.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(MaxEmailLength, ErrorMessage = "El correo no puede superar 256 caracteres.")]
    public string Email { get; init; } = string.Empty;

    [StringLength(MaxPhoneLength, ErrorMessage = "El teléfono no puede superar 30 caracteres.")]
    public string? Phone { get; init; }

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; init; } = string.Empty;
}


public sealed record OwnerAccountResponse(Guid Id, string Identification, string Name, string Email);


public sealed record UpdateUserRequest
{
    public const int MaxNameLength = 150;
    public const int MaxEmailLength = 256;
    public const int MaxPhoneLength = 30;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(MaxNameLength, ErrorMessage = "El nombre no puede superar 150 caracteres.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(MaxEmailLength, ErrorMessage = "El correo no puede superar 256 caracteres.")]
    public string Email { get; init; } = string.Empty;

    [StringLength(MaxPhoneLength, ErrorMessage = "El teléfono no puede superar 30 caracteres.")]
    public string? Phone { get; init; }
}

public sealed record UserResponse(Guid Id, string Identification, string Name, string Email, string? Phone, string Role);
