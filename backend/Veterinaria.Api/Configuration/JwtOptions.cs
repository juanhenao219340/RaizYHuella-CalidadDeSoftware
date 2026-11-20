using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    // HS256 requires a key of at least 256 bits (RNF-SEG-05, assumption S-1).
    [Required(AllowEmptyStrings = false)]
    [MinLength(32)]
    public string Key { get; set; } = string.Empty;
}
