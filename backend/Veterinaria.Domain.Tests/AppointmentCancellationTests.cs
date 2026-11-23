using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;
using Veterinaria.Domain.ValueObjects;
using Xunit;
using static Veterinaria.Domain.Tests.TestData;

namespace Veterinaria.Domain.Tests;

public class AppointmentCancellationTests
{
    [Fact]
    public void Cancel_ByAdministrator_FreesSlot()
    {

        var others = ConfirmedInSlot(3);
        var appointment = Appointment.Schedule(
            Guid.NewGuid(),
            ReferenceNumber.Parse("RAD-20261119-0001"),
            Request(),
            NowUtc,
            Options(),
            Array.Empty<AppointmentSummary>());

        var fullSlot = new List<AppointmentSummary>(others) { Summarize(appointment) };
        Assert.Equal(4, SlotCapacityCalculator.CountConfirmed(fullSlot, FutureDate, appointment.StartTime, ServiceA));
        AssertFailed(SchedulingRules.Validate(Request(), NowUtc, Options(), fullSlot, null), "RN-03");

        appointment.Cancel(UserRole.Administrator, "Anulada por el administrador", NowUtc);

        var afterCancel = new List<AppointmentSummary>(others) { Summarize(appointment) };
        Assert.Equal(3, SlotCapacityCalculator.CountConfirmed(afterCancel, FutureDate, appointment.StartTime, ServiceA));
        Assert.True(SchedulingRules.Validate(Request(), NowUtc, Options(), afterCancel, null).IsSuccess);
    }

    [Fact]
    public void Cancel_SetsReasonStatusAndVersion()
    {
        var appointment = Appointment.Schedule(
            Guid.NewGuid(),
            ReferenceNumber.Parse("RAD-20261119-0002"),
            Request(),
            NowUtc,
            Options(),
            Array.Empty<AppointmentSummary>());

        Assert.Equal(0, appointment.Version);

        appointment.Cancel(UserRole.Administrator, "  Motivo de prueba  ", NowUtc);

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal("Motivo de prueba", appointment.ChangeReason);
        Assert.Equal(NowUtc, appointment.ChangedAt);
        Assert.Equal(1, appointment.Version);
    }
}
