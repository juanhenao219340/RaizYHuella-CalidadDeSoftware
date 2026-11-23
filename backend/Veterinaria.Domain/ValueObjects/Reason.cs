namespace Veterinaria.Domain.ValueObjects;

public static class Reason
{
    public const int MaxLength = 500;




    public static string? Normalize(string? text)
    {
        var clean = text?.Trim();

        return string.IsNullOrEmpty(clean) || clean.Length > MaxLength ? null : clean;
    }
}
