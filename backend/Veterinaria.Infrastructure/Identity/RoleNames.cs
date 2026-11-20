using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Infrastructure.Identity;

public static class RoleNames
{
    public const string Administrator = "Administrator";
    public const string Owner = "Owner";

    public static readonly Guid AdministratorId = new("6f1a0c2e-1b7d-4c8a-9e21-000000000001");
    public static readonly Guid OwnerId = new("6f1a0c2e-1b7d-4c8a-9e21-000000000002");

    public static string FromRole(UserRole role) => role switch
    {
        UserRole.Administrator => Administrator,
        UserRole.Owner => Owner,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown user role.")
    };

    public static UserRole ToRole(string roleName) => roleName switch
    {
        Administrator => UserRole.Administrator,
        Owner => UserRole.Owner,
        _ => throw new InvalidOperationException($"Unknown role name '{roleName}' in Identity.")
    };
}
