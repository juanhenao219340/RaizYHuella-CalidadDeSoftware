using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.Contracts;
using Veterinaria.Application.Services;
using Veterinaria.Domain.Entities;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/services")]
public sealed class ServicesController(
    CreateServiceHandler createServiceHandler,
    UpdateServiceHandler updateServiceHandler,
    GetServiceHandler getServiceHandler,
    ListServicesHandler listServicesHandler,
    DeleteServiceHandler deleteServiceHandler) : ControllerBase
{
    [HttpGet]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<ServiceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var services = await listServicesHandler.HandleAsync(cancellationToken);

        return Ok(services.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var service = await getServiceHandler.HandleAsync(id, cancellationToken);

        return service is null ? NotFoundService() : Ok(ToResponse(service));
    }

    // Catalogue writes are administrator-only (policy). Owners receive 403 (CA-REG-05).
    [HttpPost]
    [Authorize(Policy = "Administrator")]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] ServiceRequest request, CancellationToken cancellationToken)
    {
        var service = await createServiceHandler.HandleAsync(
            new CreateServiceCommand(request.Name, request.Type!.Value),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ToResponse(service));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Administrator")]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ServiceRequest request, CancellationToken cancellationToken)
    {
        var service = await updateServiceHandler.HandleAsync(
            new UpdateServiceCommand(id, request.Name, request.Type!.Value),
            cancellationToken);

        return service is null ? NotFoundService() : Ok(ToResponse(service));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await deleteServiceHandler.HandleAsync(id, cancellationToken);

        return deleted ? NoContent() : NotFoundService();
    }

    private ObjectResult NotFoundService() =>
        NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "El servicio no existe o no está disponible."
        });

    private static ServiceResponse ToResponse(Service service) =>
        new(service.Id, service.Name, service.Type);
}
