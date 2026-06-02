using Mars.Core.Features;

namespace Test.Mars.Core;

public class TimeSpanParserTests
{
    [Theory]
    [InlineData(600_000, "10m")] // 10 minutes
    [InlineData(1_500, "1.5s")] // 1.5 seconds
    [InlineData(7_200_000, "2h")] // 2 hours
    [InlineData(259_200_000, "3d")] // 3 days
    [InlineData(250, "250ms")] // 250 milliseconds
    public void Parse_ValidInputs_ReturnsCorrectTimeSpan(double expectedMs, string input)
    {
        // Arrange
        var expected = TimeSpan.FromMilliseconds(expectedMs);

        // Act
        var result = TimeSpanParser.Parse(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parse_InvalidFormat_ThrowsFormatException()
    {
        // Arrange
        const string input = "abc";

        // Act & Assert
        Assert.Throws<FormatException>(() => TimeSpanParser.Parse(input));
    }

    [Fact]
    public void TryParse_ValidInput_ReturnsTrueAndParsedValue()
    {
        // Arrange
        const string input = "5m";

        // Act
        var success = TimeSpanParser.TryParse(input, out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(TimeSpan.FromMinutes(5), result);
    }

    [Fact]
    public void TryParse_InvalidInput_ReturnsFalse()
    {
        // Arrange
        const string input = "invalid";

        // Act
        var success = TimeSpanParser.TryParse(input, out var result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Theory]
    [InlineData(600_000, "10m")]
    [InlineData(1_500, "1.5s")]
    [InlineData(7_200_000, "2h")]
    [InlineData(259_200_000, "3d")]
    [InlineData(250, "250ms")]
    [InlineData(90_000, "1.5m")]
    [InlineData(129_600_000, "1.5d")]
    [InlineData(0, "0ms")]
    public void Format_ReturnsExpectedString(
       double milliseconds,
       string expected)
    {
        // Arrange
        var value = TimeSpan.FromMilliseconds(milliseconds);

        // Act
        var result = TimeSpanParser.Format(value);

        // Assert
        Assert.Equal(expected, result);
    }
}
