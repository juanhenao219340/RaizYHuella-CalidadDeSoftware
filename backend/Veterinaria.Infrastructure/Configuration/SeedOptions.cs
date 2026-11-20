using Microsoft.Extensions.Configuration;

namespace Veterinaria.Infrastructure.Configuration;

public sealed class SeedOptions
{
    // SEED_ADMIN_EMAIL, SEED_ADMIN_PASSWORD, SEED_ADMIN_IDENTIFICATION and SEED_ADMIN_NAME are mandatory and have
    // no default value in code (R13-6): if any is missing or empty, the API does not start.
    public string Email { get; private set; } = string.Empty;

    public string Password { get; private set; } = string.Empty;

    public string Identification { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public IReadOnlyList<string> UndefinedVariables { get; private init; } = [];

    public static SeedOptions Load(IConfiguration configuration)
    {
        var undefined = new List<string>();

        return new SeedOptions
        {
            Email = Read(configuration, "SEED_ADMIN_EMAIL", undefined),
            Password = Read(configuration, "SEED_ADMIN_PASSWORD", undefined),
            Identification = Read(configuration, "SEED_ADMIN_IDENTIFICATION", undefined),
            Name = Read(configuration, "SEED_ADMIN_NAME", undefined),
            UndefinedVariables = undefined
        };
    }

    public static void Validate(SeedOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Email))
        {
            throw new InvalidOperationException("SEED_ADMIN_EMAIL must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException("SEED_ADMIN_PASSWORD must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.Identification))
        {
            throw new InvalidOperationException("SEED_ADMIN_IDENTIFICATION must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.Name))
        {
            throw new InvalidOperationException("SEED_ADMIN_NAME must not be empty.");
        }
    }

    private static string Read(IConfiguration configuration, string key, List<string> undefined)
    {
        var value = configuration[key];

        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        undefined.Add(key);
        return string.Empty;
    }
}
