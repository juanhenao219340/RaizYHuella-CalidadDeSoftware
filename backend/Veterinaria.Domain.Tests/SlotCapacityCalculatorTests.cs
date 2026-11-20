using Veterinaria.Domain.Rules;
using Xunit;

namespace Veterinaria.Domain.Tests;

public class SlotCapacityCalculatorTests
{
    [Fact]
    public void Slots_MoreConfirmedThanCapacity_ReturnsZero()
    {
        var slots = SlotCapacityCalculator.Slots(4, 5);

        Assert.Equal(0, slots);
    }

    [Fact]
    public void Slots_ConfirmedBelowCapacity_ReturnsRemaining()
    {
        var slots = SlotCapacityCalculator.Slots(4, 3);

        Assert.Equal(1, slots);
    }

    [Fact]
    public void Slots_NegativeConfirmed_Throws()
    {
        var exception = Assert.Throws<DomainRuleException>(() => SlotCapacityCalculator.Slots(4, -1));

        Assert.Equal("RN-02", exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Slots_CapacityZeroOrNegative_Throws(int capacity)
    {
        var exception = Assert.Throws<DomainRuleException>(() => SlotCapacityCalculator.Slots(capacity, 0));

        Assert.Equal("RN-02", exception.Code);
    }

    [Fact]
    public void IsAvailable_ZeroSlots_ReturnsFalse()
    {
        Assert.False(SlotCapacityCalculator.IsAvailable(0));
    }

    [Fact]
    public void IsAvailable_OneSlot_ReturnsTrue()
    {
        Assert.True(SlotCapacityCalculator.IsAvailable(1));
    }
}
