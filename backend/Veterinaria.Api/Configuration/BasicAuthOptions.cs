using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Configuration;

public sealed class BasicAuthOptions
{
    public const string SectionName = "BasicAuth";

    [Required(AllowEmptyStrings = false)]
    public string Username { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Password { get; set; } = string.Empty;
}
