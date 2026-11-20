using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Authentication;

// UserId and Role are only meaningful when Outcome is Succeeded.
public sealed record LoginResult(LoginOutcome Outcome, Guid? UserId, UserRole? Role);
