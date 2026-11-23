namespace Veterinaria.Domain.Configuration;

public sealed class LockoutOptions
{

    public int MaxFailedAttempts { get; set; }

    public int LockoutMinutes { get; set; }
}
