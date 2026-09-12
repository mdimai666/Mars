using EditorJsBlazored.Blocks;
using EditorJsBlazored.Core;

namespace Test.EditorJsBlazored;

public class HtmlToBlocksConverterTests
{
    [Fact]
    public void Convert_TwoParagraphs_MustGiveTwoBlocks()
    {
        //Act
        var blocks = HtmlToBlocksConverter.Convert("<p>text</p><p>123</p>");

        //Assert
        Assert.Equal(2, blocks.Length);
        Assert.All(blocks, b => Assert.Equal("paragraph", b.Type));
        Assert.Equal("text", ((BlockParagraph)blocks[0].Data).Text);
        Assert.Equal("123", ((BlockParagraph)blocks[1].Data).Text);
    }

    [Fact]
    public void Convert_MockPostShape_MustNotCollapseToSingleRaw()
    {
        //Arrange — форма контента из CreateMockPostsAct
        var html = "<p>Одно предложение.</p><p>Второе.</p><p>Третье.</p>";

        //Act
        var blocks = HtmlToBlocksConverter.Convert(html);

        //Assert
        Assert.Equal(3, blocks.Length);
        Assert.DoesNotContain(blocks, b => b.Type == "raw");
    }

    [Theory]
    [InlineData("<h1>T</h1>", 1)]
    [InlineData("<h3>T</h3>", 3)]
    [InlineData("<h6>T</h6>", 6)]
    public void Convert_Heading_MustMapLevel(string html, int level)
    {
        //Act
        var blocks = HtmlToBlocksConverter.Convert(html);

        //Assert
        var header = Assert.Single(blocks);
        Assert.Equal("header", header.Type);
        var data = (BlockHeader)header.Data;
        Assert.Equal(level, data.Level);
        Assert.Equal("T", data.Text);
    }

    [Fact]
    public void Convert_UnorderedList_MustMapItemsAndStyle()
    {
        //Act
        var blocks = HtmlToBlocksConverter.Convert("<ul><li>a</li><li>b</li></ul>");

        //Assert
        var list = Assert.Single(blocks);
        Assert.Equal("list", list.Type);
        var data = (BlockList)list.Data;
        Assert.Equal("unordered", data.Style);
        Assert.Equal(new[] { "a", "b" }, data.Items);
    }

    [Fact]
    public void Convert_OrderedList_MustMapStyle()
    {
        //Act
        var blocks = HtmlToBlocksConverter.Convert("<ol><li>a</li></ol>");

        //Assert
        var data = (BlockList)Assert.Single(blocks).Data;
        Assert.Equal("ordered", data.Style);
    }

    [Fact]
    public void Convert_Blockquote_MustMapToQuote()
    {
        //Act
        var blocks = HtmlToBlocksConverter.Convert("<blockquote>q</blockquote>");

        //Assert
        var block = Assert.Single(blocks);
        Assert.Equal("quote", block.Type);
        Assert.Equal("q", ((BlockQuote)block.Data).Text);
    }

    [Fact]
    public void Convert_PreCode_MustMapToCodeWithoutTags()
    {
        //Act
        var blocks = HtmlToBlocksConverter.Convert("<pre><code>var x = 1 &amp; 2;</code></pre>");

        //Assert
        var block = Assert.Single(blocks);
        Assert.Equal("code", block.Type);
        Assert.Equal("var x = 1 & 2;", ((BlockCode)block.Data).Code);
    }

    [Fact]
    public void Convert_Hr_MustMapToDelimiter()
    {
        //Act
        var blocks = HtmlToBlocksConverter.Convert("<p>a</p><hr><p>b</p>");

        //Assert
        Assert.Equal(3, blocks.Length);
        Assert.Equal("delimiter", blocks[1].Type);
    }

    [Fact]
    public void Convert_Img_MustMapToImageWithUrlAndCaption()
    {
        //Act
        var blocks = HtmlToBlocksConverter.Convert("<img src=\"/a.png\" alt=\"A\">");

        //Assert
        var block = Assert.Single(blocks);
        Assert.Equal("image", block.Type);
        var data = (BlockImage)block.Data;
        Assert.Equal("/a.png", data.File!.Url);
        Assert.Equal("A", data.Caption);
    }

    [Fact]
    public void Convert_Table_MustMapRowsAndHeading()
    {
        //Act
        var blocks = HtmlToBlocksConverter.Convert("<table><tr><th>h</th></tr><tr><td>c</td></tr></table>");

        //Assert
        var block = Assert.Single(blocks);
        Assert.Equal("table", block.Type);
        var data = (BlockTable)block.Data;
        Assert.True(data.WithHeading);
        Assert.Equal(2, data.Rows);
        Assert.Equal(new[] { new[] { "h" }, new[] { "c" } }, data.Content);
    }

    [Fact]
    public void Convert_InlineTagsInsideParagraph_MustBePreservedInText()
    {
        //Act
        var blocks = HtmlToBlocksConverter.Convert("<p>a <strong>b</strong> <a href=\"/x\">c</a></p>");

        //Assert
        var block = Assert.Single(blocks);
        Assert.Equal("paragraph", block.Type);
        Assert.Equal("a <strong>b</strong> <a href=\"/x\">c</a>", ((BlockParagraph)block.Data).Text);
    }

    [Fact]
    public void Convert_TopLevelInlineTags_MustGroupIntoParagraphNotRaw()
    {
        //Act
        var blocks = HtmlToBlocksConverter.Convert("Hello <strong>world</strong>");

        //Assert
        var block = Assert.Single(blocks);
        Assert.Equal("paragraph", block.Type);
        Assert.Equal("Hello <strong>world</strong>", ((BlockParagraph)block.Data).Text);
    }

    [Fact]
    public void Convert_Containers_MustRecurseIntoChildren()
    {
        //Act
        var blocks = HtmlToBlocksConverter.Convert("<div><section><p>a</p><p>b</p></section></div>");

        //Assert
        Assert.Equal(2, blocks.Length);
        Assert.All(blocks, b => Assert.Equal("paragraph", b.Type));
    }

    [Fact]
    public void Convert_UnknownBlockTag_MustFallBackToRaw()
    {
        //Act
        var blocks = HtmlToBlocksConverter.Convert("<marquee>hello</marquee>");

        //Assert
        var block = Assert.Single(blocks);
        Assert.Equal("raw", block.Type);
        var html = ((BlockRaw)block.Data).Html;
        Assert.Contains("marquee", html);
        Assert.Contains("hello", html);
    }

    [Fact]
    public void Convert_MixedSequence_MustKeepOrder()
    {
        //Act
        var blocks = HtmlToBlocksConverter.Convert("<h1>T</h1><p>a</p><ul><li>x</li></ul><hr>");

        //Assert
        Assert.Equal(new[] { "header", "paragraph", "list", "delimiter" },
                     blocks.Select(b => b.Type).ToArray());
    }

    [Fact]
    public void Convert_Comments_MustBeSkipped()
    {
        //Act
        var blocks = HtmlToBlocksConverter.Convert("<p>a</p><!-- note --><p>b</p>");

        //Assert
        Assert.Equal(2, blocks.Length);
    }

    [Fact]
    public void Convert_EmptyInput_MustGiveNoBlocks()
    {
        Assert.Empty(HtmlToBlocksConverter.Convert(""));
        Assert.Empty(HtmlToBlocksConverter.Convert("   "));
    }

    [Fact]
    public void Convert_AllTypesRegistered_MustBeResolvable()
    {
        //Arrange
        var html = "<h1>T</h1><p>a</p><ul><li>x</li></ul><blockquote>q</blockquote>"
                   + "<pre><code>c</code></pre><hr><img src=\"/a.png\"><table><tr><td>c</td></tr></table>"
                   + "<marquee>m</marquee>";

        //Act
        var blocks = HtmlToBlocksConverter.Convert(html);

        //Assert — каждый выданный тип должен уметь отрендериться обратно в HTML
        Assert.NotEmpty(blocks);
        foreach (var block in blocks)
        {
            var type = EditorTools.ResolveBlockByType(block.Type);
            Assert.NotNull(type);
        }

        var rendered = EditorTools.RenderToHtml(new EditorJsContent { Blocks = blocks });
        Assert.Contains("<h1>T</h1>", rendered);
        Assert.Contains("<marquee>", rendered);
    }
}

public class FromJsonAutoConvertToBlocksTests
{
    [Fact]
    public void FromJsonAutoConvertToBlocks_ValidJson_MustNotReplace()
    {
        //Arrange
        var json = """{"blocks":[{"type":"paragraph","data":{"text":"hi"}}],"time":0,"version":"0.0.0"}""";

        //Act
        var content = EditorJsContent.FromJsonAutoConvertToBlocks(json, out var isReplaced);

        //Assert
        Assert.False(isReplaced);
        var block = Assert.Single(content.Blocks);
        Assert.Equal("paragraph", block.Type);
    }

    [Fact]
    public void FromJsonAutoConvertToBlocks_Html_MustSplitIntoBlocks()
    {
        //Act
        var content = EditorJsContent.FromJsonAutoConvertToBlocks("<p>text</p><p>123</p>", out var isReplaced);

        //Assert
        Assert.True(isReplaced);
        Assert.Equal(2, content.Blocks.Length);
        Assert.All(content.Blocks, b => Assert.Equal("paragraph", b.Type));
    }

    [Fact]
    public void FromJsonAutoConvertToBlocks_NonWhitelistedTagHtml_MustNotEscapeIntoVisibleText()
    {
        //Arrange — до правки <strong> не попадал в белый список LooksLikeHtml,
        // уходил в CreateParagraps и экранировался в видимый текст
        var html = "<strong>bold</strong>";

        //Act
        var content = EditorJsContent.FromJsonAutoConvertToBlocks(html, out var isReplaced);

        //Assert
        Assert.True(isReplaced);
        var block = Assert.Single(content.Blocks);
        Assert.Equal("paragraph", block.Type);
        var text = ((BlockParagraph)block.Data).Text;
        Assert.Contains("<strong>", text);
        Assert.DoesNotContain("&lt;", text);
    }

    [Fact]
    public void FromJsonAutoConvertToBlocks_PlainTextWithSpaces_MustStayText()
    {
        //Act
        var content = EditorJsContent.FromJsonAutoConvertToBlocks("a < b and c > d", out var isReplaced);

        //Assert
        Assert.True(isReplaced);
        var block = Assert.Single(content.Blocks);
        Assert.Equal("paragraph", block.Type);
        Assert.Contains("&lt;", ((BlockParagraph)block.Data).Text);
    }

    [Fact]
    public void FromJsonAutoConvertToBlocks_PlainText_MustSplitByBlankLine()
    {
        //Act
        var content = EditorJsContent.FromJsonAutoConvertToBlocks("first\n\nsecond", out var isReplaced);

        //Assert
        Assert.True(isReplaced);
        Assert.Equal(2, content.Blocks.Length);
    }

    [Fact]
    public void FromJsonAutoConvertToBlocks_Empty_MustNotReplace()
    {
        //Act
        var content = EditorJsContent.FromJsonAutoConvertToBlocks("", out var isReplaced);

        //Assert
        Assert.False(isReplaced);
        Assert.Empty(content.Blocks);
    }

    [Fact]
    public void FromJsonAutoConvertToBlocks_RoundTrip_MustRenderSameText()
    {
        //Arrange
        var html = "<h2>Title</h2><p>Body text</p>";

        //Act
        var content = EditorJsContent.FromJsonAutoConvertToBlocks(html, out _);
        var rendered = EditorTools.RenderToHtml(content);

        //Assert
        Assert.Contains("<h2>Title</h2>", rendered);
        Assert.Contains("<p>Body text</p>", rendered);
    }
}
