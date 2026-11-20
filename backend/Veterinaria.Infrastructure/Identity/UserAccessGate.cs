using Microsoft.AspNetCore.Identity;
using Veterinaria.Application.Abstractions;

namespace Veterinaria.Infrastructure.Identity;

public sealed class UserAccessGate(UserManager<ApplicationUser> userManager) : IUserAccessGate
{
    // One primary-key lookup per protected request (R-16). No caching: revocation must take effect immediately.
    public async Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken)
    {
        var applicationUser = await userManager.FindByIdAsync(userId.ToString());

        return applicationUser is not null && applicationUser.DeletedAt is null;
    }
}
