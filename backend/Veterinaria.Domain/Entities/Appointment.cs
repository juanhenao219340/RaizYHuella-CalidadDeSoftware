using Veterinaria.Domain.Configuration;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;
using Veterinaria.Domain.ValueObjects;

namespace Veterinaria.Domain.Entities;

public class Appointment
{
    public Guid Id { get; private set; }


    public string ReferenceNumber { get; private set; } = string.Empty;

    public Guid PetId { get; private set; }

    public Guid ServiceId { get; private set; }

    public DateOnly Date { get; private set; }

    public TimeSlot StartTime { get; private set; }

    public AppointmentStatus Status { get; private set; }

    public string? ChangeReason { get; private set; }

    public DateTimeOffset? ChangedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }


    public int Version { get; private set; }


    private Appointment()
    {
    }

    public static Appointment Schedule(
        Guid id,
        ReferenceNumber referenceNumber,
        AppointmentRequest request,
        DateTimeOffset nowUtc,
        BusinessRulesOptions options,
        IReadOnlyCollection<AppointmentSummary> appointments)
    {
        SchedulingRules.Validate(request, nowUtc, options, appointments, null).ThrowIfFailed();

        return new Appointment
        {
            Id = id,
            ReferenceNumber = referenceNumber.Value,
            PetId = request.PetId,
            ServiceId = request.ServiceId,
            Date = request.Date,
            StartTime = request.StartTime,
            Status = AppointmentStatus.Confirmed,
            CreatedAt = nowUtc,
            Version = 0
        };
    }

    public void Cancel(UserRole actor, string? reason, DateTimeOffset changedAt)
    {
        AppointmentModificationRules.ValidateCancellation(actor, this, reason).ThrowIfFailed();

        Status = AppointmentStatus.Cancelled;
        ChangeReason = Reason.Normalize(reason);
        ChangedAt = changedAt;
        Version++;
    }

    public void Update(
        UserRole actor,
        string? reason,
        DateTimeOffset changedAt,
        AppointmentRequest updated,
        DateTimeOffset nowUtc,
        BusinessRulesOptions options,
        IReadOnlyCollection<AppointmentSummary> appointments)
    {
        AppointmentModificationRules.ValidateUpdate(actor, this, reason, updated, nowUtc, options, appointments).ThrowIfFailed();

        Date = updated.Date;
        StartTime = updated.StartTime;
        ServiceId = updated.ServiceId;
        ChangeReason = Reason.Normalize(reason);
        ChangedAt = changedAt;
        Version++;
    }
}
