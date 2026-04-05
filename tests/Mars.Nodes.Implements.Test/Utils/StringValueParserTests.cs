using FluentAssertions;
using Mars.Nodes.Core.StringFunctions;

namespace Mars.Nodes.Implements.Test.Utils;

public class StringValueParserTests
{
    [Theory]
    [InlineData(typeof(string), "test", "test")]
    [InlineData(typeof(int), "123", 123)]
    [InlineData(typeof(bool), "true", true)]
    [InlineData(typeof(double), "3.14", 3.14)]
    [InlineData(typeof(decimal), "10.5", 10.5)]
    [InlineData(typeof(DateTime), "2024-01-15", "2024-01-15")]
    [InlineData(typeof(byte), "255", (byte)255)]
    [InlineData(typeof(short), "32767", (short)32767)]
    [InlineData(typeof(long), "9223372036854775807", 9223372036854775807L)]
    public void ParseByType_ValidInput_ReturnsCorrectValue(Type type, string input, object expected)
    {
        // Act
        var result = StringValueParser.ParseByType(type, input);

        // Assert
        if (expected is string expectedString && result is DateTime)
        {
            ((DateTime)result).Should().Be(DateTime.Parse(expectedString));
        }
        else
        {
            result.Should().Be(expected);
        }
    }

    [Theory]
    [InlineData(typeof(string), "", "")]
    [InlineData(typeof(int), "", 0)]
    [InlineData(typeof(bool), "", false)]
    [InlineData(typeof(double), "", 0.0)]
    [InlineData(typeof(DateTime), "", "0001-01-01")]
    public void ParseByType_EmptyString_ReturnsDefaultValue(Type type, string input, object expected)
    {
        // Act
        var result = StringValueParser.ParseByType(type, input);

        // Assert
        if (expected is string expectedString && result is DateTime)
        {
            ((DateTime)result).Should().Be(DateTime.Parse(expectedString));
        }
        else
        {
            result.Should().Be(expected);
        }
    }

    [Theory]
    [InlineData(typeof(int), "abc")]
    [InlineData(typeof(bool), "not-bool")]
    [InlineData(typeof(DateTime), "invalid-date")]
    public void ParseByType_InvalidInput_ThrowsException(Type type, string input)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => StringValueParser.ParseByType(type, input));
    }

    [Theory]
    [InlineData(typeof(int), "456", true, 456)]
    [InlineData(typeof(int), "invalid", false, 0)]
    [InlineData(typeof(bool), "true", true, true)]
    [InlineData(typeof(bool), "bad", false, false)]
    public void TryParseByType_ReturnsExpectedResult(Type type, string input, bool expectedSuccess, object expectedValue)
    {
        // Act
        var success = StringValueParser.TryParseByType(type, input, out var result);

        // Assert
        success.Should().Be(expectedSuccess);
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void IsSupportedType_ReturnsCorrectResult()
    {
        // Assert
        StringValueParser.IsSupportedType(typeof(string)).Should().BeTrue();
        StringValueParser.IsSupportedType(typeof(int)).Should().BeTrue();
        StringValueParser.IsSupportedType(typeof(Guid)).Should().BeFalse();
        StringValueParser.IsSupportedType(typeof(List<int>)).Should().BeFalse();
    }
}
