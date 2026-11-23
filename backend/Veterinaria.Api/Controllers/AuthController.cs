using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.Authentication;
using Veterinaria.Api.Contracts;
using Veterinaria.Api.Middleware;
using Veterinaria.Application.Authentication;
using Veterinaria.Application.Users;
using Veterinaria.Infrastructure.Identity;

namespace Veterinaria.Api.Controllers;



[Route("api/auth")]
public sealed class AuthController(
    LoginHandler loginHandler,
    BasicCredentialsVerifier basicCredentials,
    JwtTokenService tokenService,
    GetCurrentUserHandler getCurrentUserHandler) : ControllerBase
{


    private static ProblemDetails InvalidCredentialsProblem() => new()
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Credenciales no válidas.",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
    };


    private static ProblemDetails SessionProblem() => new()
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Sesión no válida o caducada.",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
    };


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


        var current = await getCurrentUserHandler.HandleAsync(userId.Value, cancellationToken);

        if (current is null)
        {
            return Unauthorized(SessionProblem());
        }

        return Ok(new MeResponse(userId.Value, role, current.Email, current.Name));
    }



    private IActionResult ValidationFailed()
    {
        return ValidationProblem(ModelErrorTranslator.CreateValidationProblem(ModelState, HttpContext));
    }
}
