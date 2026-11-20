using Veterinaria.Domain.Entities;

namespace Veterinaria.Infrastructure.Identity;

public static class UserMapper
{
    public static User ToDomain(ApplicationUser applicationUser, IList<string> roles)
    {
        if (roles.Count != 1)
        {
            throw new InvalidOperationException(
                $"Integrity error: user '{applicationUser.Id}' must have exactly one role, but has {roles.Count}.");
        }

        return User.Reconstitute(
            applicationUser.Id,
            applicationUser.Identification,
            applicationUser.Name,
            applicationUser.Email ?? string.Empty,
            applicationUser.Phone,
            RoleNames.ToRole(roles[0]),
            applicationUser.DeletedAt);
    }

    public static void CopyDomainData(User user, ApplicationUser applicationUser)
    {
        applicationUser.Identification = user.Identification;
        applicationUser.Name = user.Name;
        applicationUser.Phone = user.Phone;
        applicationUser.DeletedAt = user.DeletedAt;
    }
}
