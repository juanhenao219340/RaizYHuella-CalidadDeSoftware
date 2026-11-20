using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.Authentication;
using Veterinaria.Api.Contracts;
using Veterinaria.Api.Middleware;
using Veterinaria.Application.Authentication;
using Veterinaria.Application.Users;
using Veterinaria.Infrastructure.Identity;

namespace Veterinaria.Api.Controllers;

// [ApiController] is intentionally omitted: it validates the body before the action runs, and the order
// must be Basic Auth -> body validation -> Identity (D4). Body validation is therefore explicit below.
[Route("api/auth")]
public sealed class AuthController(
    LoginHandler loginHandler,
    BasicCredentialsVerifier basicCredentials,
    JwtTokenService tokenService,
    GetCurrentUserHandler getCurrentUserHandler) : ControllerBase
{
    // Same body for every credential failure, so the response does not reveal whether an e-mail exists (RNF-SEG-06).
    // A new instance per response: ProblemDetails is mutable and must not be shared.
    private static ProblemDetails InvalidCredentialsProblem() => new()
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Credenciales no válidas.",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
    };

    // Same body as the JWT OnChallenge gate in Program.cs (session/token failures on /me).
    private static ProblemDetails SessionProblem() => new()
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Sesión no válida o caducada.",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
    };

    // RN-05 lockout (R15-1, D-1, D-2, D-5): 423 with the remaining time, also sent as Retry-After.
    private static ProblemDetails LockedOutProblem(int retryAfterSeconds)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status423Locked,
            Title = "Cuenta bloqueada por intentos fallidos.",
            Detail = LockoutDetail(retryAfterSeconds),
            Type = "https://tools.ietf.org/html/rfc4918#section-11.3"
        };
        problem.Extensions["retryAfterSeconds"] = retryAfterSeconds;
        problem.Extensions["code"] = "RN-05";
        return problem;
    }

    // "Intente de nuevo en {N} minutos y {M} segundos." with singular/plural and zero parts omitted.
    private static string LockoutDetail(int seconds)
    {
        var minutes = seconds / 60;
        var remainderSeconds = seconds % 60;

        if (minutes == 0)
        {
            return $"Intente de nuevo en {Plural(remainderSeconds, "segundo", "segundos")}.";
        }

        if (remainderSeconds == 0)
        {
            return $"Intente de nuevo en {Plural(minutes, "minuto", "minutos")}.";
        }

        return $"Intente de nuevo en {Plural(minutes, "minuto", "minutos")} y {Plural(remainderSeconds, "segundo", "segundos")}.";
    }

    private static string Plural(int count, string singular, string plural) =>
        $"{count} {(count == 1 ? singular : plural)}";

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login([FromBody] LoginRequest? request, CancellationToken cancellationToken)
    {
        if (!basicCredentials.IsValid(Request))
        {
            return Unauthorized(InvalidCredentialsProblem());
        }

        if (request is null)
        {
            ModelState.AddModelError(string.Empty, "El cuerpo de la solicitud es obligatorio.");
            return ValidationFailed();
        }

        if (!TryValidateModel(request))
        {
            return ValidationFailed();
        }

        var result = await loginHandler.HandleAsync(request.Email, request.Password, cancellationToken);

        // A lockout without a known remaining time cannot be described; it falls back to the generic 401 (RNF-SEG-06).
        if (result.Outcome is LoginOutcome.LockedOut && result.RetryAfterSeconds is { } retryAfterSeconds)
        {
            Response.Headers["Retry-After"] = retryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return StatusCode(StatusCodes.Status423Locked, LockedOutProblem(retryAfterSeconds));
        }

        if (result.Outcome is not LoginOutcome.Succeeded || result.UserId is not { } userId || result.Role is not { } role)
        {
            return Unauthorized(InvalidCredentialsProblem());
        }


        var token = tokenService.Issue(userId, role);
        var roleName = RoleNames.FromRole(role);

        return Ok(new LoginResponse(token.Token, token.ExpiresAt, roleName));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindUserId();
        var role = User.FindRole();

        if (userId is null || role is null)
        {
            return Unauthorized(SessionProblem());
        }

        // Null when the account no longer exists or is logically deleted (RF-REG-10): same 401 as an invalid identity.
        var current = await getCurrentUserHandler.HandleAsync(userId.Value, cancellationToken);

        if (current is null)
        {
            return Unauthorized(SessionProblem());
        }

        return Ok(new MeResponse(userId.Value, role, current.Email, current.Name));
    }

    // Same shape as the InvalidModelStateResponseFactory in Program.cs (translated field errors, Spanish title),
    // so manual validation never leaks framework text (RNF-IDI-01, CA-REG-09).
    private IActionResult ValidationFailed()
    {
        return ValidationProblem(ModelErrorTranslator.CreateValidationProblem(ModelState, HttpContext));
    }
}
