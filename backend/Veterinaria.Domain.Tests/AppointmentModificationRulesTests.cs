using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;
using Veterinaria.Domain.ValueObjects;
using Xunit;
using static Veterinaria.Domain.Tests.TestData;

namespace Veterinaria.Domain.Tests;

public class AppointmentModificationRulesTests
{
    [Fact]
    public void Cancel_ByOwner_ReturnsRN04WithMessage()
    {
        var appointment = ScheduledAppointment();

        var result = AppointmentModificationRules.ValidateCancellation(UserRole.Owner, appointment, "Motivo válido");

        AssertFailed(result, "RN-04");
        Assert.Equal("Solo el administrador puede anular citas.", result.Error!.Message);
    }

    [Fact]
    public void Cancel_ByOwnerEntityCall_ThrowsRN04AndKeepsAppointmentConfirmed()
    {
        var appointment = ScheduledAppointment();

        var exception = Assert.Throws<DomainRuleException>(
            () => appointment.Cancel(UserRole.Owner, "Motivo válido", NowUtc));

        Assert.Equal("RN-04", exception.Code);
        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
        Assert.Equal(0, appointment.Version);
    }

    [Fact]
    public void Update_ByOwner_ReturnsRN04()
    {
        var appointment = ScheduledAppointment();

        var result = ValidateUpdate(UserRole.Owner, appointment, "Motivo válido");

        AssertFailed(result, "RN-04");
        Assert.Equal("Solo el administrador puede actualizar citas.", result.Error!.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancel_EmptyOrWhitespaceReason_ReturnsRN04(string? reason)
    {
        var result = AppointmentModificationRules.ValidateCancellation(UserRole.Administrator, ScheduledAppointment(), reason);

        AssertFailed(result, "RN-04");
        Assert.Equal("El motivo es obligatorio y no puede superar 500 caracteres.", result.Error!.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_EmptyOrWhitespaceReason_ReturnsRN04(string? reason)
    {
        var result = ValidateUpdate(UserRole.Administrator, ScheduledAppointment(), reason);

        AssertFailed(result, "RN-04");
    }

    [Fact]
    public void Cancel_Reason500Characters_IsAccepted()
    {
        var appointment = ScheduledAppointment();
        var reason = new string('a', 500);

        appointment.Cancel(UserRole.Administrator, reason, NowUtc);

        Assert.Equal(reason, appointment.ChangeReason);
    }

    [Fact]
    public void Cancel_Reason501Characters_ReturnsRN04()
    {
        var result = AppointmentModificationRules.ValidateCancellation(
            UserRole.Administrator,
            ScheduledAppointment(),
            new string('a', 501));

        AssertFailed(result, "RN-04");
    }

    [Fact]
    public void Cancel_ReasonTrimmedBeforeLimit_IsAccepted()
    {

        var appointment = ScheduledAppointment();
        var letters = new string('a', 498);

        appointment.Cancel(UserRole.Administrator, $"  {letters}  ", NowUtc);

        Assert.Equal(letters, appointment.ChangeReason);
    }

    [Fact]
    public void Update_CancelledAppointment_ReturnsRF_PRO_07()
    {
        var appointment = ScheduledAppointment();
        appointment.Cancel(UserRole.Administrator, "Motivo válido", NowUtc);

        var result = ValidateUpdate(UserRole.Administrator, appointment, "Motivo válido");

        AssertFailed(result, "RF-PRO-07");
    }

    [Fact]
    public void Cancel_CancelledAppointment_ReturnsRF_PRO_07EvenWithValidReason()
    {
        var appointment = ScheduledAppointment();
        appointment.Cancel(UserRole.Administrator, "Motivo válido", NowUtc);

        var result = AppointmentModificationRules.ValidateCancellation(UserRole.Administrator, appointment, "Otro motivo válido");

        AssertFailed(result, "RF-PRO-07");
    }

    [Fact]
    public void Cancel_OwnerOnCancelledAppointment_ReturnsRN04FirstBecauseRoleIsEvaluatedFirst()
    {
        var appointment = ScheduledAppointment();
        appointment.Cancel(UserRole.Administrator, "Motivo válido", NowUtc);

        var result = AppointmentModificationRules.ValidateCancellation(UserRole.Owner, appointment, "Motivo válido");

        AssertFailed(result, "RN-04");
    }

    private static Appointment ScheduledAppointment() =>
        Appointment.Schedule(
            Guid.NewGuid(),
            ReferenceNumber.Parse("RAD-20261119-0100"),
            Request(),
            NowUtc,
            Options(),
            Array.Empty<AppointmentSummary>());

    private static DomainResult ValidateUpdate(UserRole actor, Appointment appointment, string? reason) =>
        AppointmentModificationRules.ValidateUpdate(
            actor,
            appointment,
            reason,
            Request(serviceId: ServiceB),
            NowUtc,
            Options(),
            Array.Empty<AppointmentSummary>());
}
