using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.Authentication;
using Veterinaria.Api.Contracts;
using Veterinaria.Application.Appointments;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public sealed class AppointmentsController(
    GetAvailabilityHandler getAvailabilityHandler,
    ListAppointmentsHandler listAppointmentsHandler,
    CreateAppointmentHandler createAppointmentHandler,
    RescheduleAppointmentHandler rescheduleAppointmentHandler,
    CancelAppointmentHandler cancelAppointmentHandler) : ControllerBase
{
    // Same body as the JWT OnChallenge gate in Program.cs. A new instance per response: ProblemDetails is mutable.
    private static ProblemDetails SessionProblem() => new()
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Sesión no válida o caducada.",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
    };

    private const string DateFormat = "yyyy-MM-dd";

    private const string ReferenceNumberKey = "referenceNumber";
    private const string PetIdentifierKey = "petIdentifier";
    private const string OwnerIdentificationKey = "ownerIdentification";
    private const string ServiceTypeKey = "serviceType";

    private sealed record ListFilters(string? ReferenceNumber, string? PetIdentifier, string? OwnerIdentification, ServiceType? ServiceType);

    private static readonly Regex ReferenceNumberPattern = new(AppointmentSearchLimits.ReferenceNumberPattern, RegexOptions.CultureInvariant);

    // All roles with a valid token can read: administrators see everything, owners only their own (RF-CON-05).
    // Filters are optional and combined with AND. ownerId and unknown parameters are ignored (RF-PRO-09, RF-CON-06).
    [HttpGet]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<AppointmentListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!TryGetCaller(out var callerId, out var callerRole))
        {
            return Unauthorized(SessionProblem());
        }

        if (!TryReadListFilters(out var filters, out var problem))
        {
            return BadRequest(problem);
        }

        var appointments = await listAppointmentsHandler.HandleAsync(
            new ListAppointmentsQuery(
                callerId,
                callerRole,
                filters.ReferenceNumber,
                filters.PetIdentifier,
                filters.OwnerIdentification,
                filters.ServiceType),
            cancellationToken);

        return Ok(appointments.Select(ToListItemResponse).ToList());
    }

    // Read-only for administrators and owners (D-A). Routed before any {id:guid} route.
    [HttpGet("availability")]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<SlotAvailabilityResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetAvailability(
        [FromQuery, Required(ErrorMessage = "El servicio es obligatorio.")] Guid? serviceId,
        [FromQuery, Required(ErrorMessage = "La fecha es obligatoria.")] string? date,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(date, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["date"] = ["La fecha debe tener el formato yyyy-MM-dd."]
            })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Los datos enviados no son válidos."
            });
        }

        if (!TryGetCaller(out _, out var callerRole))
        {
            return Unauthorized(SessionProblem());
        }

        var slots = await getAvailabilityHandler.HandleAsync(new GetAvailabilityQuery(serviceId!.Value, day, callerRole), cancellationToken);

        if (slots is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "El servicio no existe o no está disponible."
            });
        }

        return Ok(slots.Select(slot => new SlotAvailabilityResponse(slot.StartTime.ToString(), slot.RemainingSlots, slot.Available)).ToList());
    }

    // Owner only. OwnerId is the JWT subject, never the body (RF-PRO-09).
    [HttpPost]
    [Authorize(Policy = "Owner")]
    [ProducesResponseType<AppointmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCaller(out var callerId, out _))
        {
            return Unauthorized(SessionProblem());
        }

        var appointment = await createAppointmentHandler.HandleAsync(
            new CreateAppointmentCommand(
                callerId,
                request.PetId!.Value,
                request.ServiceId!.Value,
                request.Date!.Value,
                request.StartTime!),
            cancellationToken);

        return appointment is null
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "La mascota o el servicio no existen o no están disponibles."
            })
            : StatusCode(StatusCodes.Status201Created, ToResponse(appointment));
    }

    // Administrator only. The caller's role is passed so the domain re-asserts it (RF-PRO-06).
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Administrator")]
    [ProducesResponseType<AppointmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reschedule(Guid id, [FromBody] UpdateAppointmentRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCaller(out _, out var callerRole))
        {
            return Unauthorized(SessionProblem());
        }

        var appointment = await rescheduleAppointmentHandler.HandleAsync(
            new RescheduleAppointmentCommand(
                id,
                callerRole,
                request.ServiceId!.Value,
                request.Date!.Value,
                request.StartTime!,
                request.Reason),
            cancellationToken);

        return appointment is null ? NotFoundAppointment() : Ok(ToResponse(appointment));
    }

    // Administrator only. The reason is mandatory and is stored as the last change reason (RN-04, RF-PRO-08).
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Administrator")]
    [ProducesResponseType<AppointmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelAppointmentRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCaller(out _, out var callerRole))
        {
            return Unauthorized(SessionProblem());
        }

        var appointment = await cancelAppointmentHandler.HandleAsync(
            new CancelAppointmentCommand(id, callerRole, request.Reason),
            cancellationToken);

        return appointment is null ? NotFoundAppointment() : Ok(ToResponse(appointment));
    }

    // Validation order: repeated parameter, trimming, length and characters, serviceType. The first error is returned (T-07 section 6).
    private bool TryReadListFilters(out ListFilters filters, out ValidationProblemDetails? problem)
    {
        var query = Request.Query;
        filters = new ListFilters(null, null, null, null);
        problem = null;

        if (!TryReadSingle(query, ReferenceNumberKey, out var referenceNumber, out problem)
            || !TryReadSingle(query, PetIdentifierKey, out var petIdentifier, out problem)
            || !TryReadSingle(query, OwnerIdentificationKey, out var ownerIdentification, out problem)
            || !TryReadSingle(query, ServiceTypeKey, out var serviceType, out problem))
        {
            return false;
        }

        if (referenceNumber is not null
            && (referenceNumber.Length > AppointmentSearchLimits.MaxReferenceNumberLength || !ReferenceNumberPattern.IsMatch(referenceNumber)))
        {
            problem = InvalidFilter(ReferenceNumberKey, "El radicado solo puede contener letras, números y guiones, y no superar 20 caracteres.");
            return false;
        }

        if (petIdentifier is not null && petIdentifier.Length > AppointmentSearchLimits.MaxPetIdentifierLength)
        {
            problem = InvalidFilter(PetIdentifierKey, "El identificador de la mascota no puede superar 50 caracteres.");
            return false;
        }

        if (ownerIdentification is not null && ownerIdentification.Length > AppointmentSearchLimits.MaxOwnerIdentificationLength)
        {
            problem = InvalidFilter(OwnerIdentificationKey, "La identificación del propietario no puede superar 20 caracteres.");
            return false;
        }

        ServiceType? parsedServiceType = null;

        if (serviceType is not null)
        {
            if (!Enum.GetNames<ServiceType>().Contains(serviceType, StringComparer.Ordinal))
            {
                problem = InvalidFilter(ServiceTypeKey, "El tipo de servicio debe ser Clinic, Grooming o Recreation.");
                return false;
            }

            parsedServiceType = Enum.Parse<ServiceType>(serviceType);
        }

        filters = new ListFilters(referenceNumber, petIdentifier, ownerIdentification, parsedServiceType);
        return true;
    }

    // Trims the value; blank means absent (D-7). A repeated key is rejected instead of choosing one value.
    private bool TryReadSingle(IQueryCollection query, string key, out string? value, out ValidationProblemDetails? problem)
    {
        var values = query[key];

        value = null;
        problem = null;

        if (values.Count > 1)
        {
            problem = InvalidFilter(key, "Este filtro solo puede indicarse una vez.");
            return false;
        }

        var trimmed = values.Count == 1 ? values[0]?.Trim() : null;
        value = string.IsNullOrEmpty(trimmed) ? null : trimmed;

        return true;
    }

    private ValidationProblemDetails InvalidFilter(string key, string message) =>
        new(new Dictionary<string, string[]> { [key] = [message] })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Los datos enviados no son válidos.",
            Extensions = { ["traceId"] = HttpContext.TraceIdentifier }
        };

    private static AppointmentListItemResponse ToListItemResponse(AppointmentListItem item) =>
        new(
            item.Id,
            item.ReferenceNumber,
            item.PetId,
            item.PetName,
            item.ServiceId,
            item.ServiceName,
            item.ServiceType.ToString(),
            item.Date,
            item.StartTime,
            item.Status.ToString());

    private bool TryGetCaller(out Guid callerId, out UserRole callerRole)
    {
        var userId = User.FindUserId();
        var role = User.FindUserRole();

        callerId = userId ?? Guid.Empty;
        callerRole = role ?? default;

        return userId is not null && role is not null;
    }

    // Same response for "missing" and "not visible" (no data leak).
    private ObjectResult NotFoundAppointment() =>
        NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "La cita no existe o no está disponible."
        });

    private static AppointmentResponse ToResponse(Appointment appointment) =>
        new(
            appointment.Id,
            appointment.ReferenceNumber,
            appointment.PetId,
            appointment.ServiceId,
            appointment.Date,
            appointment.StartTime.ToString(),
            appointment.Status.ToString(),
            appointment.ChangeReason,
            appointment.ChangedAt,
            appointment.CreatedAt);
}
