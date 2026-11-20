using Veterinaria.Application.Abstractions;

namespace Veterinaria.Application.Authentication;

public sealed class LoginHandler(IAuthenticationGateway authenticationGateway, TimeProvider timeProvider)
{
    public async Task<LoginResult> HandleAsync(string email, string password, CancellationToken cancellationToken)
    {
        // Null is excluded by the controller's body validation. Whitespace-only values are wrong credentials, not errors.
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);

        var result = await authenticationGateway.CheckCredentialsAsync(email, password, cancellationToken);

        if (result.Outcome is not LoginOutcome.LockedOut || result.LockoutEnd is not { } lockoutEnd)
        {
            return result;
        }

        // Seconds left in the lockout, rounded up, never less than 1 while the lock is active (RF-AUT-03, RN-05).
        var remaining = lockoutEnd - timeProvider.GetUtcNow();
        var retryAfterSeconds = (int)Math.Max(1, Math.Ceiling(remaining.TotalSeconds));

        return result with { RetryAfterSeconds = retryAfterSeconds };
    }
}
