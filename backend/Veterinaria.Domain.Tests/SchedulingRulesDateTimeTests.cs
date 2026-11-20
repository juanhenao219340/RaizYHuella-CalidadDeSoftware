using Veterinaria.Domain.Configuration;
using Veterinaria.Domain.Rules;
using Veterinaria.Domain.ValueObjects;
using Xunit;

namespace Veterinaria.Domain.Tests;

public class SchedulingRulesDateTimeTests
{
    // Injected clock: 2026-11-18 12:00 in America/Bogota (UTC-5, no DST), i.e. 17:00 UTC.
    private static readonly DateTimeOffset NowUtc = new(2026, 11, 18, 17, 0, 0, TimeSpan.Zero);

    private static readonly BusinessRulesOptions Options = new()
    {
        SlotCapacity = 4,
        TimeZone = "America/Bogota"
    };

    private static readonly DateOnly Today = new(2026, 11, 18);

    [Fact]
    public void Validate_DateBeforeToday_ReturnsRN01()
    {
        var result = Validate(Today.AddDays(-1), 15);

        AssertFailed(result, "RN-01");
    }

    [Fact]
    public void Validate_YesterdayWithLaterHour_StillReturnsRN01()
    {
        // The hour does not rescue a date that is already in the past.
        var result = Validate(Today.AddDays(-1), 16);

        AssertFailed(result, "RN-01");
    }

    [Fact]
    public void Validate_FutureDateEarlierHour_IsAccepted()
    {
        // P-01 only applies to the current day.
        var result = Validate(Today.AddDays(1), 8);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_TodayTimeAfterNow_IsAccepted()
    {
        var result = Validate(Today, 15);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_TodayTimeBeforeNow_ReturnsP01()
    {
        var result = Validate(Today, 10);

        AssertFailed(result, "P-01");
    }

    [Fact]
    public void Validate_TodayTimeEqualToNow_ReturnsP01()
    {
        // The rule requires a start time strictly after the current time.
        var result = Validate(Today, 12);

        AssertFailed(result, "P-01");
    }

    [Fact]
    public void Validate_UtcMidnightCrossesBogotaDate_UsesBogotaToday()
    {
        // 2026-11-19 03:00 UTC is 2026-11-18 22:00 in Bogota, so "today" is still the 18th.
        var nowUtc = new DateTimeOffset(2026, 11, 19, 3, 0, 0, TimeSpan.Zero);

        var nextDay = Validate(Today.AddDays(1), 8, nowUtc);
        var todayAtTen = Validate(Today, 10, nowUtc);

        Assert.True(nextDay.IsSuccess);
        // Using the UTC date (19th) would give RN-01; the Bogota date (18th) gives P-01.
        AssertFailed(todayAtTen, "P-01");
    }

    private static DomainResult Validate(DateOnly date, int hour, DateTimeOffset? nowUtc = null) =>
        SchedulingRules.Validate(
            new AppointmentRequest(Guid.NewGuid(), date, TimeSlot.Create(new TimeOnly(hour, 0)), Guid.NewGuid()),
            nowUtc ?? NowUtc,
            Options,
            Array.Empty<AppointmentSummary>(),
            null);

    private static void AssertFailed(DomainResult result, string expectedCode)
    {
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(expectedCode, result.Error.Code);
    }
}
