namespace Veterinaria.Domain.Configuration;

public sealed class LockoutOptions
{
    // Required from the environment (RNF-CFG-01). No default: the start-up validator rejects a missing value.
    public int MaxFailedAttempts { get; set; }

    public int LockoutMinutes { get; set; }
}
