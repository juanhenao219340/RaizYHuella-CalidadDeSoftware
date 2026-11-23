using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;
using Veterinaria.Domain.ValueObjects;
using Xunit;
using static Veterinaria.Domain.Tests.TestData;

namespace Veterinaria.Domain.Tests;


public class ServiceDeletionCancellationTests
{

    private static readonly DateOnly Today = new(2026, 11, 18);


    private static readonly DateTimeOffset MorningUtc = new(2026, 11, 18, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ConfirmedFuture_IsSelected_AndCancelledWithFixedReason()
    {
        var appointment = ScheduleAt(FutureDate, hour: 10, nowUtc: NowUtc);

        Assert.True(AppointmentModificationRules.IsFutureConfirmed(appointment, NowUtc, Options()));

        appointment.Cancel(UserRole.Administrator, AppointmentModificationRules.ServiceDeletedReason, NowUtc);

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal("Servicio eliminado por la clínica.", appointment.ChangeReason);
        Assert.Equal(NowUtc, appointment.ChangedAt);
        Assert.Equal(1, appointment.Version);
    }

    [Fact]
    public void ConfirmedSameDayStartingLaterToday_IsSelected()
    {

        var appointment = ScheduleAt(Today, hour: 14, nowUtc: MorningUtc);

        Assert.True(AppointmentModificationRules.IsFutureConfirmed(appointment, NowUtc, Options()));
    }

    [Fact]
    public void ConfirmedSameDayAlreadyStarted_IsNotSelected()
    {

        var appointment = ScheduleAt(Today, hour: 10, nowUtc: MorningUtc);

        Assert.False(AppointmentModificationRules.IsFutureConfirmed(appointment, NowUtc, Options()));
    }

    [Fact]
    public void AlreadyCancelled_IsNotSelected_AndCancelAgainIsRF_PRO_07()
    {
        var appointment = ScheduleAt(FutureDate, hour: 10, nowUtc: NowUtc);
        appointment.Cancel(UserRole.Administrator, "Motivo previo", NowUtc);

        Assert.False(AppointmentModificationRules.IsFutureConfirmed(appointment, NowUtc, Options()));

        var exception = Assert.Throws<DomainRuleException>(() =>
            appointment.Cancel(UserRole.Administrator, AppointmentModificationRules.ServiceDeletedReason, NowUtc));

        Assert.Equal("RF-PRO-07", exception.Code);
        Assert.Equal("Motivo previo", appointment.ChangeReason);
        Assert.Equal(1, appointment.Version);
    }

    [Fact]
    public void ServiceDeletedReason_WithAdministrator_IsAccepted()
    {
        var appointment = ScheduleAt(FutureDate, hour: 10, nowUtc: NowUtc);

        var result = AppointmentModificationRules.ValidateCancellation(
            UserRole.Administrator,
            appointment,
            AppointmentModificationRules.ServiceDeletedReason);

        Assert.True(result.IsSuccess);
        Assert.Equal("Servicio eliminado por la clínica.", Reason.Normalize(AppointmentModificationRules.ServiceDeletedReason));
    }

    [Fact]
    public void ServiceDeletedReason_WithOwner_IsRejectedByRN04()
    {
        var appointment = ScheduleAt(FutureDate, hour: 10, nowUtc: NowUtc);

        AssertFailed(
            AppointmentModificationRules.ValidateCancellation(UserRole.Owner, appointment, AppointmentModificationRules.ServiceDeletedReason),
            "RN-04");
    }

    private static Appointment ScheduleAt(DateOnly date, int hour, DateTimeOffset nowUtc) =>
        Appointment.Schedule(
            Guid.NewGuid(),
            ReferenceNumber.Parse("RAD-20261118-0001"),
            Request(date: date, hour: hour),
            nowUtc,
            Options(),
            Array.Empty<AppointmentSummary>());
}
