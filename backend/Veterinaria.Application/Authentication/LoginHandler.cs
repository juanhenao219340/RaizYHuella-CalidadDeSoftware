using Veterinaria.Application.Abstractions;

namespace Veterinaria.Application.Authentication;

public sealed class LoginHandler(IAuthenticationGateway authenticationGateway)
{
    public Task<LoginResult> HandleAsync(string email, string password, CancellationToken cancellationToken)
    {
        // Null is excluded by the controller's body validation. Whitespace-only values are wrong credentials, not errors.
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);

        return authenticationGateway.CheckCredentialsAsync(email, password, cancellationToken);
    }
}
