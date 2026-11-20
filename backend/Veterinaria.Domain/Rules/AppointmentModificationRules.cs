using Veterinaria.Domain.Configuration;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.ValueObjects;

namespace Veterinaria.Domain.Rules;

public static class AppointmentModificationRules
{
    // Single RF-PRO-07 message for both the domain check and the concurrency translation in persistence (RF-PRO-07, T-06 r4).
    public const string CancelledMessage = "La cita está anulada y no puede modificarse.";

    // Automatic reason of the cancellations made when a service is deleted (R18-1). Goes through the same RN-04 validation.
    public const string ServiceDeletedReason = "Servicio eliminado por la clínica.";

    // Selection of the appointments cancelled by a service deletion: confirmed and starting after the current moment in the business time zone (D2).
    public static bool IsFutureConfirmed(Appointment appointment, DateTimeOffset nowUtc, BusinessRulesOptions options)
    {
        if (appointment.Status != AppointmentStatus.Confirmed)
        {
            return false;
        }

        var localNow = TimeZoneInfo.ConvertTime(nowUtc, options.GetTimeZone()).DateTime;

        return appointment.Date.ToDateTime(appointment.StartTime.StartTime) > localNow;
    }

    public static DomainResult ValidateCancellation(UserRole actor, Appointment appointment, string? reason) =>
        ValidateCommon(actor, appointment, reason, "anular");

    public static DomainResult ValidateUpdate(
        UserRole actor,
        Appointment appointment,
        string? reason,
        AppointmentRequest updated,
        DateTimeOffset nowUtc,
        BusinessRulesOptions options,
        IReadOnlyCollection<AppointmentSummary> appointments)
    {
        var common = ValidateCommon(actor, appointment, reason, "actualizar");

        if (!common.IsSuccess)
        {
            return common;
        }

        var request = updated with { PetId = appointment.PetId };

        return SchedulingRules.Validate(request, nowUtc, options, appointments, appointment.Id);
    }

    private static DomainResult ValidateCommon(UserRole actor, Appointment appointment, string? reason, string action)
    {
        if (actor != UserRole.Administrator)
        {
            return DomainResult.Failed("RN-04", $"Solo el administrador puede {action} citas.");
        }

        if (Reason.Normalize(reason) is null)
        {
            return DomainResult.Failed("RN-04", "El motivo es obligatorio y no puede superar 500 caracteres.");
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return DomainResult.Failed("RF-PRO-07", CancelledMessage);
        }

        return DomainResult.Success();
    }
}
