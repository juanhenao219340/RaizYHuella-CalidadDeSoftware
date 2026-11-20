using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace Veterinaria.Api.Middleware;

// Answers policy failures (role mismatch) with a Spanish 403 body (RNF-IDI-01, D-12, OD-3).
public sealed class ForbiddenResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler fallback = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult policyAuthorizationResult)
    {
        if (!policyAuthorizationResult.Forbidden)
        {
            await fallback.HandleAsync(next, context, policy, policyAuthorizationResult);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "No tiene permiso para realizar esta acción.",
            Detail = "Su rol no tiene permiso para realizar esta acción.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4"
        });
    }
}
