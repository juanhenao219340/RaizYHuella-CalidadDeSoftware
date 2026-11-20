using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.Authentication;
using Veterinaria.Api.Contracts;
using Veterinaria.Application.Users;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(
    CreateOwnerAccountHandler createOwnerAccountHandler,
    ListOwnersHandler listOwnersHandler,
    GetCurrentUserHandler getCurrentUserHandler,
    UpdateCurrentUserHandler updateCurrentUserHandler,
    GetUserHandler getUserHandler,
    UpdateUserHandler updateUserHandler,
    DeleteUserHandler deleteUserHandler) : ControllerBase
{
    // Same body as the JWT OnChallenge gate in Program.cs. A new instance per response: ProblemDetails is mutable.
    private static ProblemDetails SessionProblem() => new()
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Sesión no válida o caducada.",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
    };

    // Only administrators can create owner accounts (RF-AUT-10, CA-AUT-07). The role is fixed by the server.
    [HttpPost]
    [Authorize(Policy = "Administrator")]
    [ProducesResponseType<OwnerAccountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOwner([FromBody] CreateOwnerRequest request, CancellationToken cancellationToken)
    {
        var user = await createOwnerAccountHandler.HandleAsync(
            new CreateOwnerAccountCommand(
                request.Identification,
                request.Name,
                request.Email,
                request.Phone,
                request.Password),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new OwnerAccountResponse(user.Id, user.Identification, user.Name, user.Email));
    }

    // Owner accounts only (RF-REG-07). Administrators are never listed.
    [HttpGet]
    [Authorize(Policy = "Administrator")]
    [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListOwners(CancellationToken cancellationToken)
    {
        var owners = await listOwnersHandler.HandleAsync(cancellationToken);

        return Ok(owners.Select(ToResponse).ToList());
    }

    // Self-service profile (RF-REG-08): the role is read-only and never comes from the request.
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var callerId = User.FindUserId();

        if (callerId is null)
        {
            return Unauthorized(SessionProblem());
        }

        var current = await getCurrentUserHandler.HandleAsync(callerId.Value, cancellationToken);

        return current is null ? NotFoundAccount() : Ok(ToResponse(current));
    }

    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCurrent([FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var callerId = User.FindUserId();

        if (callerId is null)
        {
            return Unauthorized(SessionProblem());
        }

        var updated = await updateCurrentUserHandler.HandleAsync(
            new UpdateCurrentUserCommand(callerId.Value, request.Name, request.Email, request.Phone),
            cancellationToken);

        return updated is null ? NotFoundAccount() : Ok(ToResponse(updated));
    }

    // Visibility rules (owner sees only self, D-4 -> 404; administrator on administrator -> 403) live in Application.
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCaller(out var callerId, out var callerRole))
        {
            return Unauthorized(SessionProblem());
        }

        var user = await getUserHandler.HandleAsync(
            new GetUserQuery(id, callerId, callerRole), cancellationToken);

        return user is null ? NotFoundAccount() : Ok(ToResponse(user));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCaller(out var callerId, out var callerRole))
        {
            return Unauthorized(SessionProblem());
        }

        var updated = await updateUserHandler.HandleAsync(
            new UpdateUserCommand(id, callerId, callerRole, request.Name, request.Email, request.Phone),
            cancellationToken);

        return updated is null ? NotFoundAccount() : Ok(ToResponse(updated));
    }

    // Logical deletion (RF-REG-10). Returns 403 when an owner targets their own account (c3).
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCaller(out var callerId, out var callerRole))
        {
            return Unauthorized(SessionProblem());
        }

        var deleted = await deleteUserHandler.HandleAsync(
            new DeleteUserCommand(id, callerId, callerRole), cancellationToken);

        return deleted ? NoContent() : NotFoundAccount();
    }

    private bool TryGetCaller(out Guid callerId, out UserRole callerRole)
    {
        var userId = User.FindUserId();
        var role = User.FindUserRole();

        callerId = userId ?? Guid.Empty;
        callerRole = role ?? default;

        return userId is not null && role is not null;
    }

    private ObjectResult NotFoundAccount() =>
        NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "La cuenta no existe o no está disponible."
        });

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.Identification, user.Name, user.Email, user.Phone, user.Role.ToString());
}
