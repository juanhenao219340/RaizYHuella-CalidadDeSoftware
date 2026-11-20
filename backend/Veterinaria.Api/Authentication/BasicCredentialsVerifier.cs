using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Veterinaria.Api.Configuration;

namespace Veterinaria.Api.Authentication;

public sealed class BasicCredentialsVerifier(IOptions<BasicAuthOptions> options)
{
    private const string Scheme = "Basic ";

    // Only used by /api/auth/login (RNF-SEG-04 and Q-1 accepted risk). Never answers with WWW-Authenticate,
    // so the browser does not show its credentials dialog. Failures are not logged (OD-8).
    public bool IsValid(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();

        if (!header.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var encoded = header[Scheme.Length..].Trim();
        var buffer = new byte[encoded.Length];

        if (!Convert.TryFromBase64String(encoded, buffer, out var written))
        {
            return false;
        }

        var decoded = Encoding.UTF8.GetString(buffer, 0, written);
        var separator = decoded.IndexOf(':');

        if (separator < 0)
        {
            return false;
        }

        var username = Encoding.UTF8.GetBytes(decoded[..separator]);
        var password = Encoding.UTF8.GetBytes(decoded[(separator + 1)..]);

        // Non-short-circuit AND keeps the comparison time independent of which part is wrong.
        var expected = options.Value;
        var usernameMatches = CryptographicOperations.FixedTimeEquals(username, Encoding.UTF8.GetBytes(expected.Username));
        var passwordMatches = CryptographicOperations.FixedTimeEquals(password, Encoding.UTF8.GetBytes(expected.Password));

        return usernameMatches & passwordMatches;
    }
}
