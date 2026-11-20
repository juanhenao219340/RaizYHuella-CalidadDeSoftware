using Microsoft.AspNetCore.Identity;
using Veterinaria.Application.Abstractions;
using Veterinaria.Application.Authentication;

namespace Veterinaria.Infrastructure.Identity;

public sealed class IdentityAuthenticationGateway(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IAuthenticationGateway
{
    public async Task<LoginResult> CheckCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        // Unknown or deleted e-mail: same outcome as a wrong password, and Identity does not count it (OD-9, RF-REG-10).
        var applicationUser = await userManager.FindByEmailAsync(email);

        if (applicationUser is null || applicationUser.DeletedAt is not null)
        {
            return new LoginResult(LoginOutcome.InvalidCredentials, null, null);
        }

        // lockoutOnFailure applies the per-account lockout (RF-AUT-07, RN-05). It does not sign in with a cookie.
        var result = await signInManager.CheckPasswordSignInAsync(applicationUser, password, lockoutOnFailure: true);

        // Identity checks the lockout before the password and does not count attempts made during it,
        // so the lockout end is not extended by them (RN-05, D-4).
        if (result.IsLockedOut)
        {
            var lockoutEnd = await userManager.GetLockoutEndDateAsync(applicationUser);
            return new LoginResult(LoginOutcome.LockedOut, null, null, lockoutEnd);
        }

        if (!result.Succeeded)
        {
            return new LoginResult(LoginOutcome.InvalidCredentials, null, null);
        }

        var roles = await userManager.GetRolesAsync(applicationUser);
        var user = UserMapper.ToDomain(applicationUser, roles);

        return new LoginResult(LoginOutcome.Succeeded, user.Id, user.Role);
    }
}
