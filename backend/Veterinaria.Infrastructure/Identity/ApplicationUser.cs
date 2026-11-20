using Microsoft.AspNetCore.Identity;

namespace Veterinaria.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string Identification { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    // Logical deletion marker (RF-REG-10). Null while the account is active.
    public DateTimeOffset? DeletedAt { get; set; }
}
