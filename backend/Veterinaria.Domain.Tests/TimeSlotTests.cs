using Veterinaria.Domain.ValueObjects;
using Xunit;

namespace Veterinaria.Domain.Tests;

public class TimeSlotTests
{
    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(17)]
    public void Create_WholeHourBetween0800And1700_IsAccepted(int hour)
    {
        var slot = TimeSlot.Create(new TimeOnly(hour, 0));

        Assert.Equal(new TimeOnly(hour, 0), slot.StartTime);
    }

    [Theory]
    [InlineData(7, 0)]
    [InlineData(18, 0)]
    [InlineData(8, 30)]
    [InlineData(17, 30)]
    [InlineData(0, 0)]
    public void Create_OutsideRangeOrNotWholeHour_ThrowsRF_PRO_01(int hour, int minute)
    {
        var exception = Assert.Throws<Veterinaria.Domain.Rules.DomainRuleException>(
            () => TimeSlot.Create(new TimeOnly(hour, minute)));

        Assert.Equal("RF-PRO-01", exception.Code);
    }
}
