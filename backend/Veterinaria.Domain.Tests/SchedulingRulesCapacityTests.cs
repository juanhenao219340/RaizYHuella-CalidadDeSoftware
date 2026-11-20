using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;
using Xunit;
using static Veterinaria.Domain.Tests.TestData;

namespace Veterinaria.Domain.Tests;

public class SchedulingRulesCapacityTests
{
    [Fact]
    public void Validate_ThreeConfirmedOfFour_IsAccepted()
    {
        // Four capacity minus three confirmed leaves one slot.
        var result = Validate(ConfirmedInSlot(3));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_FourConfirmedOfFour_ReturnsRN03()
    {
        var result = Validate(ConfirmedInSlot(4));

        AssertFailed(result, "RN-03");
    }

    [Fact]
    public void Validate_CancelledAppointments_DoNotCount()
    {
        var appointments = Enumerable.Range(0, 4)
            .Select(_ => Summary(AppointmentStatus.Cancelled))
            .ToList();

        var result = Validate(appointments);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_SameSlotOtherService_DoesNotCount()
    {
        var result = Validate(ConfirmedInSlot(4, serviceId: ServiceB));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_SameServiceOtherHour_DoesNotCount()
    {
        var result = Validate(ConfirmedInSlot(4, hour: 9));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_RescheduleExcludesOwnAppointmentFromCount()
    {
        // Four confirmed in the slot, but one of them is the appointment being rescheduled.
        var appointments = ConfirmedInSlot(4);
        var ownId = appointments[0].Id;

        var result = SchedulingRules.Validate(
            Request(),
            NowUtc,
            Options(),
            appointments,
            ownId);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_CapacityZeroOrNegative_ReturnsRN02(int capacity)
    {
        var result = SchedulingRules.Validate(
            Request(),
            NowUtc,
            Options(capacity),
            Array.Empty<AppointmentSummary>(),
            null);

        AssertFailed(result, "RN-02");
    }

    private static DomainResult Validate(IReadOnlyCollection<AppointmentSummary> appointments) =>
        SchedulingRules.Validate(Request(), NowUtc, Options(), appointments, null);
}
