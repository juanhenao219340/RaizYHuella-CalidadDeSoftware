using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Authentication;




public sealed record LoginResult(
    LoginOutcome Outcome,
    Guid? UserId,
    UserRole? Role,
    DateTimeOffset? LockoutEnd = null,
    int? RetryAfterSeconds = null);
