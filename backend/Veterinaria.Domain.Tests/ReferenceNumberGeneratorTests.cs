using Veterinaria.Domain.Rules;
using Xunit;

namespace Veterinaria.Domain.Tests;

public class ReferenceNumberGeneratorTests
{
    private static readonly DateOnly RegistrationDate = new(2026, 11, 18);

    [Fact]
    public void Format_FirstSequence_ReturnsRAD0001()
    {
        var reference = ReferenceNumberGenerator.Format(RegistrationDate, 1);

        Assert.Equal("RAD-20261118-0001", reference.Value);
    }

    [Fact]
    public void Format_Sequence9999_IsValid()
    {
        var reference = ReferenceNumberGenerator.Format(RegistrationDate, 9999);

        Assert.Equal("RAD-20261118-9999", reference.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    [InlineData(-1)]
    public void Format_SequenceOutOfRange_ThrowsRF_PRO_03(int sequence)
    {
        var exception = Assert.Throws<DomainRuleException>(() => ReferenceNumberGenerator.Format(RegistrationDate, sequence));

        Assert.Equal("RF-PRO-03", exception.Code);
    }
}
