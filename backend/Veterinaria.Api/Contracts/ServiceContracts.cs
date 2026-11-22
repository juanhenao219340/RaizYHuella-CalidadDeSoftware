using System.ComponentModel.DataAnnotations;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Api.Contracts;

public sealed record ServiceRequest
{
    public const int MaxNameLength = 100;

    [Required(ErrorMessage = "El nombre del servicio es obligatorio.")]
    [StringLength(MaxNameLength, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Name { get; init; } = string.Empty;

    // Serialized as text: Clinic, Aesthetics or Recreation (D-10, JsonStringEnumConverter in Program.cs).
    [Required(ErrorMessage = "El tipo de servicio es obligatorio.")]
    public ServiceType? Type { get; init; }
}

public sealed record ServiceResponse(Guid Id, string Name, ServiceType Type);
