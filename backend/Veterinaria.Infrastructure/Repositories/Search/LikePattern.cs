namespace Veterinaria.Infrastructure.Repositories.Search;

// Builds a "contains" LIKE pattern for ILIKE. The escape character is "\" and every wildcard typed by the user is escaped,
// so "%" or "_" never match everything (T-07 section 5).
public static class LikePattern
{
    public const string EscapeCharacter = "\\";

    public static string Contains(string value)
    {
        var escaped = value
            .Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter, StringComparison.Ordinal)
            .Replace("%", EscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", EscapeCharacter + "_", StringComparison.Ordinal);

        return $"%{escaped}%";
    }
}
