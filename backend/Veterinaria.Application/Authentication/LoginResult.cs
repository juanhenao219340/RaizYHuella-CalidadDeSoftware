using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Authentication;

// UserId and Role are only meaningful when Outcome is Succeeded.
// LockoutEnd is set by the gateway when Outcome is LockedOut (Identity's lockout end, UTC).
// RetryAfterSeconds is filled by LoginHandler from LockoutEnd and TimeProvider (RN-05, R15-1).
public sealed record LoginResult(
    LoginOutcome Outcome,
    Guid? UserId,
    UserRole? Role,
    DateTimeOffset? LockoutEnd = null,
    int? RetryAfterSeconds = null);
