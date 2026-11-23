using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";


    [Required(AllowEmptyStrings = false)]
    [MinLength(32)]
    public string Key { get; set; } = string.Empty;
}
