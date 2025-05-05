using Mars.Core.Extensions;
using Mars.Core.Features;

namespace Test.Mars.Core;

public class TextHelperTests
{
    /// <summary>
    /// <see cref="TextHelper.ParseBracketPairs(string)"/>
    /// </summary>
    [Fact]
    public void ParseBracketPairs()
    {
        string input = "(1+(2+3))";
        var result = TextHelper.ParseBracketPairs(input);
        //  start   end     depth
        //  3	    7       1
        //  0       8       0

        BracketPair[] expect = new BracketPair[]
        {
            new BracketPair(3, 7, 1),
            new BracketPair(0, 8, 0),
        };

        Assert.Equivalent(expect, result);
    }

    /// <summary>
    /// <see cref="TextHelper.ParseChainPair(string)"/>
    /// </summary>
    [Fact]
    public void ParseChainPair()
    {
        string input = "ef.Posts.Where(s=>s == 1+(2+3)).ToList()";
        var result = TextHelper.ParseChainPair(input);

        //  start   end     depth   Method  Argument
        //  14  30  0   Where   s=>s == 1 + (2 + 3)
        //  38  39  0   ToList
        ChainPair[] expect = new ChainPair[]
        {
            new ChainPair(14,30,0,"Where","s=>s == 1+(2+3)"),
            new ChainPair(38,39,0,"ToList",""),
        };

        Assert.Equivalent(expect, result);

    }

    /// <summary>
    /// <see cref="TextHelper.ParseChainPairKeyValue(string)"/>
    /// </summary>
    [Fact]
    public void ParseChainPairKeyValue()
    {
        string input = "ef.Posts.Where(s=>s == 1+(2+3)).ToList()";
        var result = TextHelper.ParseChainPairKeyValue(input);

        KeyValuePair<string, string>[] expect = new KeyValuePair<string, string>[]
        {
            new KeyValuePair<string, string>("Where","s=>s == 1+(2+3)"),
            new KeyValuePair<string, string>("ToList","")
        };
        Assert.Equivalent(expect, result);

    }


    /// <summary>
    /// <see cref="TextHelper.ParseArguments(string)"/>
    /// </summary>
    [Fact]
    public void ParseArguments()
    {
        string input = "Func1(s=>s == 1+(2+3), 777)";
        var result = TextHelper.ParseArguments(input);

        string[] expect = new string[]
        {
            "s=>s == 1+(2+3)",
            "777"
        };
        Assert.Equivalent(expect, result);

    }

    [Fact]
    public void SplitArguments()
    {
        string input = "(\"Hello, world!\"),post.Title==\"testPost1\", (333,111)";
        var result = TextHelper.SplitArguments(input);

        string[] expect = new string[]
        {
            "(\"Hello, world!\")",
            "post.Title==\"testPost1\"",
            "(333,111)"
        };

        Assert.Equivalent(expect, result);

        string input2 = "Home, Edit, HDMI (4K, ARC), Test, Double 2.0 x1, Multi (F1, (f2-f5, f6, 58), FF, o2), ARG";
        var result2 = TextHelper.SplitArguments(input2);

        string[] expect2 = new string[]
        {
            "Home",
            "Edit",
            "HDMI (4K, ARC)",
            "Test",
            "Double 2.0 x1",
            "Multi (F1, (f2-f5, f6, 58), FF, o2)",
            "ARG",
        };

        Assert.Equivalent(expect2, result2);

    }

    [Fact]
    public void IsSlug()
    {
        string[] valid = ["post", "post-name", "post_name01", "post.1", "_post_", "1post2"];
        string[] notValis = [null!, "", "-post", ".post.", "пост", "a__!@#$%^&*()+=__z", "(z)"];

        foreach (string val in valid)
        {
            Assert.True(Tools.IsValidSlug(val));
        }
        foreach (string val in notValis)
        {
            Assert.False(Tools.IsValidSlug(val));
        }
    }
}
