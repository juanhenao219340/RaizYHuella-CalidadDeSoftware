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

        var applicationUser = await userManager.FindByEmailAsync(email);

        if (applicationUser is null || applicationUser.DeletedAt is not null)
        {
            return new LoginResult(LoginOutcome.InvalidCredentials, null, null);
        }


        var result = await signInManager.CheckPasswordSignInAsync(applicationUser, password, lockoutOnFailure: true);



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
