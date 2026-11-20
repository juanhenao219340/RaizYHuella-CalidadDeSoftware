using Veterinaria.Domain.ValueObjects;
using Xunit;

namespace Veterinaria.Domain.Tests;

public class ReasonTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_NullEmptyOrWhitespace_ReturnsNull(string? text)
    {
        Assert.Null(Reason.Normalize(text));
    }

    [Fact]
    public void Normalize_500Characters_ReturnsText()
    {
        var text = new string('a', 500);

        Assert.Equal(text, Reason.Normalize(text));
    }

    [Fact]
    public void Normalize_501Characters_ReturnsNull()
    {
        Assert.Null(Reason.Normalize(new string('a', 501)));
    }

    [Fact]
    public void Normalize_SurroundingSpaces_ReturnsTrimmedText()
    {
        Assert.Equal("Motivo", Reason.Normalize("  Motivo  "));
    }

    [Fact]
    public void MaxLength_IsFiveHundred()
    {
        Assert.Equal(500, Reason.MaxLength);
    }
}
