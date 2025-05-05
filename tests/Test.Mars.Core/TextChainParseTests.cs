using Mars.Core.Features;
using Xunit.Abstractions;

namespace Test.Mars.Core;

public class TextChainParseTests
{
    private readonly ITestOutputHelper output;


    string exp1 = "post.Where(post.Tags.Includes(\"адаптация\"))";
    //string exp2 = "post.Where(post.Tags.Includes(\"адаптация\") && 1>2)";

    KeyValuePair<string, string>[] chain1 = new[] { new KeyValuePair<string, string>("Where", "post.Tags.Includes(\"адаптация\")") };

    public TextChainParseTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void ChainParse()
    {

        var parse = TextHelper.ParseChainPair(exp1).ToList();

        output.WriteLine($"Method[0] = {parse[0].Method}");
        Assert.Equal("Where", parse[0].Method);

        ChainPair[] expect = new[]
        {
            new ChainPair(0, 0, 0, "Where", "post.Tags.Includes(\"адаптация\")")
        };

        Assert.Equal(expect[0].Method, parse[0].Method);
    }


}


