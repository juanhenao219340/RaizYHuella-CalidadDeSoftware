using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Veterinaria.Api.Configuration;

namespace Veterinaria.Api.Authentication;

public sealed class BasicCredentialsVerifier(IOptions<BasicAuthOptions> options)
{
    private const string Scheme = "Basic ";



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


        var expected = options.Value;
        var usernameMatches = CryptographicOperations.FixedTimeEquals(username, Encoding.UTF8.GetBytes(expected.Username));
        var passwordMatches = CryptographicOperations.FixedTimeEquals(password, Encoding.UTF8.GetBytes(expected.Password));

        return usernameMatches & passwordMatches;
    }
}
