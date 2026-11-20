namespace Veterinaria.Domain.ValueObjects;

public static class Reason
{
    public const int MaxLength = 500;

    /// <summary>
    /// Returns the reason without leading or trailing spaces, or null if it is empty after trimming or exceeds 500 characters.
    /// </summary>
    public static string? Normalize(string? text)
    {
        var clean = text?.Trim();

        return string.IsNullOrEmpty(clean) || clean.Length > MaxLength ? null : clean;
    }
}
