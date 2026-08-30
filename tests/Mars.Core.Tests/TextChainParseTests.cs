using Mars.Core.Features;

namespace Mars.Core.Tests;

public class TextChainParseTests
{
    string exp1 = "post.Where(post.Tags.Includes(\"адаптация\"))";

    [Fact]
    public void ParseChainPair_WhereWithIncludes_ReturnsSingleSegment()
    {
        ChainPair[] expect =
        [
            new(0, 0, 0, "Where", "post.Tags.Includes(\"адаптация\")")
        ];

        var parse = TextHelper.ParseChainPair(exp1).ToList();

        Assert.Equal("Where", parse[0].Method);
        Assert.Equal(expect[0].Method, parse[0].Method);
    }
}
