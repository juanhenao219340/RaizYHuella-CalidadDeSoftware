using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Domain.Rules;

namespace Veterinaria.Api.Middleware;

public sealed class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    // Conflicts with existing data (OD-6, RF-PRO-07 enmienda r9). RF-REG-06 covers duplicated e-mail/identification.
    private static readonly HashSet<string> ConflictCodes =
    [
        "RN-03", "P-02", "RF-PRO-07", "RF-PRO-11", "RF-REG-06"
    ];

    // Permission rules (RF-PRO-09): administrator over administrator, owner over own account, owner deleting own account.
    private static readonly HashSet<string> ForbiddenCodes =
    [
        "RF-PRO-09"
    ];

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is DomainRuleException domainException)
        {
            await WriteDomainErrorAsync(httpContext, domainException, cancellationToken);
            return true;
        }

        // Deleted owner during an appointment operation (RF-AUT-07): same 401 text as the JWT gate (T-05).
        if (exception is InactiveAccountException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = exception.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
            }, cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled exception for {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Ocurrió un error inesperado.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        }, cancellationToken);

        return true;
    }

    private static async Task WriteDomainErrorAsync(HttpContext httpContext, DomainRuleException exception, CancellationToken cancellationToken)
    {
        // Password policy failures are reported as validation errors so the client can show them next to the field (M-10).
        if (exception.Code == "RF-AUT-06")
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["password"] = [exception.Message]
            })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "La contraseña no cumple la política de seguridad.",
                Extensions = { ["code"] = exception.Code }
            }, cancellationToken);
            return;
        }

        var status = ConflictCodes.Contains(exception.Code)
            ? StatusCodes.Status409Conflict
            : ForbiddenCodes.Contains(exception.Code)
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status422UnprocessableEntity;

        var title = status switch
        {
            StatusCodes.Status409Conflict => "Conflicto con el estado actual.",
            StatusCodes.Status403Forbidden => "Acción no permitida.",
            _ => "Regla de negocio incumplida."
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Extensions = { ["code"] = exception.Code }
        }, cancellationToken);
    }
}
