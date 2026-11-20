using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Contracts;

public sealed record LoginRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(256, ErrorMessage = "El correo no puede superar 256 caracteres.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; init; } = string.Empty;
}

// The JWT travels only in this body; there is no Set-Cookie (RF-AUT-03, RNF-SEG-02).
public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt, string Role);

public sealed record MeResponse(Guid UserId, string Role, string Email, string Name);
