using FluentAssertions;
using Mars.QueryLang.Services;

namespace Mars.QueryLang.Tests;

public class DataQueryBodyParserTests
{
    [Fact]
    public void FunctionBodyParse_KeyValueLines_SkipsCommentsAndEmpty()
    {
        var body = """
            posts = ef.posts.Take(3)
            // comment line
            x = =1+1

            noEqualsSign
            """;

        var result = DataQueryBodyParser.FunctionBodyParse(body);

        result.Key.Should().NotBeEmpty();
        result.Queries.Should().HaveCount(2);
        result.Queries.First().Key.Should().Be("posts");
        result.Queries.First().Value.Should().Be("ef.posts.Take(3)");
    }

    [Fact]
    public void FunctionBodyParse_ExplicitKey_Used()
    {
        var result = DataQueryBodyParser.FunctionBodyParse("a = 1", "myKey");

        result.Key.Should().Be("myKey");
    }

    [Theory]
    [InlineData("10m", 10 * 60)]
    [InlineData("1h30m", 90 * 60)]
    [InlineData("45s", 45)]
    public void ParseTimespan_ValidFormats(string input, int expectedSeconds)
    {
        DataQueryBodyParser.ParseTimespan(input).Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public void ParseTimespan_InvalidFormat_ReturnsNull()
    {
        DataQueryBodyParser.ParseTimespan("abc").Should().BeNull();
    }
}
