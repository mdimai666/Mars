using Mars.Nodes.Core.Fields;

namespace Mars.Nodes.Implements.Test.Fields;

public class InputValueTests
{
    [Fact]
    public void Parse_LiteralString_ShouldBeLiteral()
    {
        // Arrange
        var input = "hello";

        // Act
        InputValue<string> value = input;

        // Assert
        Assert.False(value.IsEvalExpression);
        Assert.False(value.IsSingleName);
        Assert.False(value.IsJsonLiteral);
        Assert.Equal("hello", value.ValueOrExpression);
        Assert.Equal("hello", value.ToString());
    }

    [Fact]
    public void Parse_ExpressionSingleName_ShouldDetectSingleName()
    {
        // Arrange
        var input = "@value";

        // Act
        var value = InputValue<string>.Parse(input);

        // Assert
        Assert.True(value.IsEvalExpression);
        Assert.True(value.IsSingleName);
        Assert.False(value.IsJsonLiteral);
        Assert.Equal("value", value.ValueOrExpression);
        Assert.Equal("@value", value.ToString());
    }

    [Fact]
    public void Parse_ExpressionComplex_ShouldNotBeSingleName()
    {
        // Arrange
        var input = "@1+1";

        // Act
        var value = InputValue<int>.Parse(input);

        // Assert
        Assert.True(value.IsEvalExpression);
        Assert.False(value.IsSingleName);
        Assert.False(value.IsJsonLiteral);
        Assert.Equal("1+1", value.ValueOrExpression);
        Assert.Equal("@1+1", value.ToString());
    }

    [Fact]
    public void Parse_EscapeAt_ShouldProduceLiteralAt()
    {
        // Arrange
        var input = "@@value";

        // Act
        var value = InputValue<string>.Parse(input);

        // Assert
        Assert.False(value.IsEvalExpression);
        Assert.False(value.IsSingleName);
        Assert.False(value.IsJsonLiteral);
        Assert.Equal("@value", value.ValueOrExpression);
        Assert.Equal("@@value", value.ToString());
    }

    [Fact]
    public void Parse_DoubleEscape_ShouldStillBeLiteral()
    {
        // Arrange
        var input = "@@@x";

        // Act
        var value = InputValue<string>.Parse(input);

        // Assert
        Assert.False(value.IsEvalExpression);
        Assert.False(value.IsSingleName);
        Assert.False(value.IsJsonLiteral);
        Assert.Equal("@x", value.ValueOrExpression);
        Assert.Equal("@@x", value.ToString());
    }

    [Fact]
    public void Parse_JsonObject_ShouldBeJsonLiteral()
    {
        // Arrange
        var input = "{\"a\":1}";

        // Act
        var value = InputValue<string>.Parse(input);

        // Assert
        Assert.False(value.IsEvalExpression);
        Assert.False(value.IsSingleName);
        Assert.True(value.IsJsonLiteral);
        Assert.Equal(input, value.ValueOrExpression);
        Assert.Equal(input, value.ToString());
    }

    [Fact]
    public void Parse_JsonArray_ShouldBeJsonLiteral()
    {
        // Arrange
        var input = "[1,2,3]";

        // Act
        var value = InputValue<string>.Parse(input);

        // Assert
        Assert.False(value.IsEvalExpression);
        Assert.False(value.IsSingleName);
        Assert.True(value.IsJsonLiteral);
        Assert.Equal(input, value.ValueOrExpression);
    }

    [Fact]
    public void Parse_JsonString_ShouldBeJsonLiteral()
    {
        // Arrange
        var input = "\"@value\"";

        // Act
        var value = InputValue<string>.Parse(input);

        // Assert
        Assert.False(value.IsEvalExpression);
        Assert.False(value.IsSingleName);
        Assert.True(value.IsJsonLiteral);
        Assert.Equal(input, value.ValueOrExpression);
        Assert.Equal(input, value.ToString());
    }

    [Fact]
    public void ImplicitConversion_FromString_ShouldParse()
    {
        // Arrange
        string input = "@test";

        // Act
        InputValue<string> value = input;

        // Assert
        Assert.True(value.IsEvalExpression);
        Assert.True(value.IsSingleName);
        Assert.Equal("test", value.ValueOrExpression);
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldCallToString()
    {
        // Arrange
        var value = InputValue<string>.Parse("@x");

        // Act
        string result = value;

        // Assert
        Assert.Equal("@x", result);
    }

    [Fact]
    public void Equal_CompareStringByStringValue_ShouldEqual()
    {
        // Arrange
        var value = InputValue<string>.Parse("@x");

        // Assert
        Assert.True("@x" == value);
        Assert.Equal("@x", value);
    }
}
