using Veterinaria.Domain.Rules;
using Veterinaria.Domain.ValueObjects;
using Xunit;

namespace Veterinaria.Domain.Tests;

public class ReferenceNumberTests
{
    [Fact]
    public void Parse_ValidFormat_ReturnsValue()
    {
        var reference = ReferenceNumber.Parse("RAD-20261118-0001");

        Assert.Equal("RAD-20261118-0001", reference.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("rad-20261118-0001")]
    [InlineData("RAD-2026118-0001")]
    [InlineData("RAD-202611188-0001")]
    [InlineData("RAD-2026AB18-0001")]
    [InlineData("RAD-20261340-0001")]
    [InlineData("RAD-20260230-0001")]
    [InlineData("RAD-20261118-0000")]
    [InlineData("RAD_20261118_0001")]
    [InlineData("RAD-20261118-0001\n")]
    [InlineData("RAD-20261118-001")]
    public void Parse_InvalidFormat_ThrowsRF_PRO_03(string? value)
    {
        var exception = Assert.Throws<DomainRuleException>(() => ReferenceNumber.Parse(value));

        Assert.Equal("RF-PRO-03", exception.Code);
    }
}
