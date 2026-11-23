using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Contracts;



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


public sealed record UpdateAppointmentRequest
{
    [Required(ErrorMessage = "El servicio es obligatorio.")]
    public Guid? ServiceId { get; init; }

    [Required(ErrorMessage = "La fecha de la cita es obligatoria.")]
    public DateOnly? Date { get; init; }

    [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
    [RegularExpression("^(0[89]|1[0-7]):00$", ErrorMessage = "La hora de inicio debe estar entre 08:00 y 17:00, en punto.")]
    public string? StartTime { get; init; }



    [Required(AllowEmptyStrings = true, ErrorMessage = "El motivo del cambio es obligatorio.")]
    public string? Reason { get; init; }
}

public sealed record CancelAppointmentRequest
{


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
    string Status,
    string? OwnerName);


public static class AppointmentSearchLimits
{
    public const int MaxReferenceNumberLength = 20;
    public const int MaxOwnerIdentificationLength = 20;
    public const int MaxPetNameLength = CreatePetRequest.MaxNameLength;

    public const string ReferenceNumberPattern = "^[A-Za-z0-9-]+\\z";
}
