using Microsoft.AspNetCore.Identity;
using DomainLockoutOptions = Veterinaria.Domain.Configuration.LockoutOptions;

namespace Veterinaria.Infrastructure.Identity;

public static class IdentityConfiguration
{
    public static void Apply(IdentityOptions options, DomainLockoutOptions lockout)
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 1;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = lockout.MaxFailedAttempts;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(lockout.LockoutMinutes);

        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = string.Empty;

        options.SignIn.RequireConfirmedAccount = false;
    }
}
