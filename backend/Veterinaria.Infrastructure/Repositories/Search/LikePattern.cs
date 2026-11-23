namespace Veterinaria.Infrastructure.Repositories.Search;



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
