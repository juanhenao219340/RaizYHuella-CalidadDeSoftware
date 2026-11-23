using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Api.Authentication;

public static class CurrentUserExtensions
{

    public static Guid? FindUserId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return Guid.TryParse(subject, out var userId) ? userId : null;
    }

    public static string? FindRole(this ClaimsPrincipal principal) =>
        principal.FindFirst(AuthenticationConstants.RoleClaim)?.Value;

    public static UserRole? FindUserRole(this ClaimsPrincipal principal) =>
        Enum.TryParse<UserRole>(principal.FindRole(), out var role) ? role : null;
}
