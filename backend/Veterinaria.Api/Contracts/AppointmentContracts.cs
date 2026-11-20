using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Contracts;

// OwnerId is never accepted from the body: it always comes from the JWT subject (RF-PRO-09). Any such field is ignored.
// Nullable value types so that a missing value yields a Spanish 400 instead of a default value.
public sealed record CreateAppointmentRequest
{
    [Required(ErrorMessage = "La mascota es obligatoria.")]
    public Guid? PetId { get; init; }

    [Required(ErrorMessage = "El servicio es obligatorio.")]
    public Guid? ServiceId { get; init; }

    [Required(ErrorMessage = "La fecha de la cita es obligatoria.")]
    public DateOnly? Date { get; init; }

    [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
    [RegularExpression("^(0[89]|1[0-7]):00$", ErrorMessage = "La hora de inicio debe estar entre 08:00 y 17:00, en punto.")]
    public string? StartTime { get; init; }
}

// The pet is not editable: the appointment keeps its pet (T-06 section 2).
public sealed record UpdateAppointmentRequest
{
    [Required(ErrorMessage = "El servicio es obligatorio.")]
    public Guid? ServiceId { get; init; }

    [Required(ErrorMessage = "La fecha de la cita es obligatoria.")]
    public DateOnly? Date { get; init; }

    [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
    [RegularExpression("^(0[89]|1[0-7]):00$", ErrorMessage = "La hora de inicio debe estar entre 08:00 y 17:00, en punto.")]
    public string? StartTime { get; init; }

    // AllowEmptyStrings: "" and blank reasons reach the domain (RN-04 -> 422). A missing value (null) stays a 400.
    // The domain also trims and enforces the 500-character limit; excess after trimming returns 422.
    [Required(AllowEmptyStrings = true, ErrorMessage = "El motivo del cambio es obligatorio.")]
    public string? Reason { get; init; }
}

public sealed record CancelAppointmentRequest
{
    // AllowEmptyStrings: "" and blank reasons reach the domain (RN-04 -> 422). A missing value (null) stays a 400.
    // The domain also trims and enforces the 500-character limit; excess after trimming returns 422.
    [Required(AllowEmptyStrings = true, ErrorMessage = "El motivo de la anulación es obligatorio.")]
    public string? Reason { get; init; }
}

public sealed record AppointmentResponse(
    Guid Id,
    string ReferenceNumber,
    Guid PetId,
    Guid ServiceId,
    DateOnly Date,
    string StartTime,
    string Status,
    string? ChangeReason,
    DateTimeOffset? ChangedAt,
    DateTimeOffset CreatedAt);

public sealed record SlotAvailabilityResponse(string StartTime, int RemainingSlots, bool Available);

// Read model for GET /api/appointments (T-07). No owner identification, e-mail or OwnerId is exposed.
public sealed record AppointmentListItemResponse(
    Guid Id,
    string ReferenceNumber,
    Guid PetId,
    string PetName,
    Guid ServiceId,
    string ServiceName,
    string ServiceType,
    DateOnly Date,
    string StartTime,
    string Status);

// Limits of the appointment search filters (T-07 section 6). Column lengths: Appointments.ReferenceNumber, Pets.Identifier, AspNetUsers.Identification.
public static class AppointmentSearchLimits
{
    public const int MaxReferenceNumberLength = 20;
    public const int MaxPetIdentifierLength = 50;
    public const int MaxOwnerIdentificationLength = 20;

    public const string ReferenceNumberPattern = "^[A-Za-z0-9-]+\\z";
}
