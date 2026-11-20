using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Configuration;

public sealed class CorsOptions : IValidatableObject
{
    public const string SectionName = "Cors";

    [Required(AllowEmptyStrings = false)]
    public string AllowedOrigin { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(AllowedOrigin))
        {
            yield break;
        }

        var isAbsolute = Uri.TryCreate(AllowedOrigin, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        if (!isAbsolute)
        {
            yield return new ValidationResult(
                "Cors:AllowedOrigin must be an absolute http or https URI.",
                [nameof(AllowedOrigin)]);
        }
    }
}
