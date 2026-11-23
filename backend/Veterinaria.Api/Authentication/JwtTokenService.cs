using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Veterinaria.Api.Configuration;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Infrastructure.Identity;

namespace Veterinaria.Api.Authentication;

public sealed record IssuedToken(string Token, DateTimeOffset ExpiresAt);

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    public IssuedToken Issue(Guid userId, UserRole role)
    {
        var now = timeProvider.GetUtcNow();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Key));
        var expiresAt = now.AddMinutes(AuthenticationConstants.TokenLifetimeMinutes);


        Claim[] claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(AuthenticationConstants.RoleClaim, RoleNames.FromRole(role))
        ];

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = AuthenticationConstants.Issuer,
            Audience = AuthenticationConstants.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new IssuedToken(token, expiresAt);
    }
}
