using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.Authentication;
using Veterinaria.Api.Contracts;
using Veterinaria.Application.Pets;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/pets")]
public sealed class PetsController(
    CreatePetHandler createPetHandler,
    UpdatePetHandler updatePetHandler,
    GetPetHandler getPetHandler,
    ListPetsHandler listPetsHandler,
    DeletePetHandler deletePetHandler) : ControllerBase
{
    // Same body as the JWT OnChallenge gate in Program.cs. A new instance per response: ProblemDetails is mutable.
    private static ProblemDetails SessionProblem() => new()
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Sesión no válida o caducada.",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
    };

    // Owners see their own pets; administrators see every non-deleted pet (read-only, OD-4).
    [HttpGet]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<PetResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!TryGetCaller(out var callerId, out var callerRole))
        {
            return Unauthorized(SessionProblem());
        }

        var pets = await listPetsHandler.HandleAsync(new ListPetsQuery(callerId, callerRole), cancellationToken);

        return Ok(pets.Select(ToResponse).ToList());

    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType<PetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCaller(out var callerId, out var callerRole))
        {
            return Unauthorized(SessionProblem());
        }

        var pet = await getPetHandler.HandleAsync(new GetPetQuery(id, callerId, callerRole), cancellationToken);

        return pet is null ? NotFoundPet() : Ok(ToResponse(pet));
    }

    // Write operations are owner-only (policy). Administrators receive 403 here (D-3).
    // OwnerId is always the JWT subject; the body cannot provide it (D-1, RF-PRO-09).
    [HttpPost]
    [Authorize(Policy = "Owner")]
    [ProducesResponseType<PetResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreatePetRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCaller(out var callerId, out _))
        {
            return Unauthorized(SessionProblem());
        }

        var pet = await createPetHandler.HandleAsync(
            new CreatePetCommand(
                request.Identifier,
                request.Name,
                request.Species,
                request.Breed,
                request.BirthDate!.Value,
                callerId),
            cancellationToken);

        // Owners never receive owner data: ownerName and ownerDeleted are null (R14-1, CA-REG-08 ii).
        return StatusCode(StatusCodes.Status201Created, ToResponse(new PetWithOwner(pet, OwnerName: null, OwnerDeleted: null)));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Owner")]
    [ProducesResponseType<PetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePetRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCaller(out var callerId, out _))
        {
            return Unauthorized(SessionProblem());
        }

        var pet = await updatePetHandler.HandleAsync(
            new UpdatePetCommand(id, callerId, request.Name, request.Species, request.Breed, request.BirthDate!.Value),
            cancellationToken);

        return pet is null ? NotFoundPet() : Ok(ToResponse(new PetWithOwner(pet, OwnerName: null, OwnerDeleted: null)));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCaller(out var callerId, out _))
        {
            return Unauthorized(SessionProblem());
        }

        var deleted = await deletePetHandler.HandleAsync(new DeletePetCommand(id, callerId), cancellationToken);

        return deleted ? NoContent() : NotFoundPet();
    }

    private bool TryGetCaller(out Guid callerId, out UserRole callerRole)
    {
        var userId = User.FindUserId();
        var role = User.FindUserRole();

        callerId = userId ?? Guid.Empty;
        callerRole = role ?? default;

        return userId is not null && role is not null;
    }

    // Same response for "missing", "deleted" and "belongs to another owner" (no data leak).
    private ObjectResult NotFoundPet() =>
        NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "La mascota no existe o no está disponible."
        });

    // Single response shape for every role: owner fields are null for owners (R14-1, RF-REG-07 a).
    private static PetResponse ToResponse(PetWithOwner petWithOwner)
    {
        var pet = petWithOwner.Pet;

        return new PetResponse(
            pet.Id,
            pet.Identifier,
            pet.Name,
            pet.Species,
            pet.Breed,
            pet.BirthDate,
            pet.OwnerId,
            petWithOwner.OwnerName,
            petWithOwner.OwnerDeleted);
    }
}
