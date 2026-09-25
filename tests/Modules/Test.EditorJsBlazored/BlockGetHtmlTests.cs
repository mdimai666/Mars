using EditorJsBlazored.Blocks;
using EditorJsBlazored.Core;

namespace Test.EditorJsBlazored;

public class BlockGetHtmlTests
{
    [Fact]
    public void BlockList_GetHtml_MustWrapItemsInLi()
    {
        //Act
        var html = new BlockList { Style = "unordered", Items = ["a", "b"] }.GetHtml();

        //Assert
        Assert.Contains("<ul>", html);
        Assert.Contains("<li>a</li>", html);
        Assert.Contains("<li>b</li>", html);
    }

    [Fact]
    public void BlockList_GetHtml_OrderedStyle_MustUseOl()
    {
        //Act
        var html = new BlockList { Style = "ordered", Items = ["a"] }.GetHtml();

        //Assert
        Assert.Contains("<ol>", html);
        Assert.Contains("<li>a</li>", html);
    }

    [Fact]
    public void BlockQuote_GetHtml_NoCaption_MustNotEmitDollarOrSeparator()
    {
        //Act
        var html = new BlockQuote { Text = "q" }.GetHtml();

        //Assert
        Assert.Contains(">q<", html);
        Assert.DoesNotContain("$", html);
        Assert.DoesNotContain(" - ", html);
    }

    [Fact]
    public void BlockQuote_GetHtml_WithCaption_MustAppendCaption()
    {
        //Act
        var html = new BlockQuote { Text = "q", Caption = "author" }.GetHtml();

        //Assert
        Assert.Contains(">q<", html);
        Assert.Contains(" - author", html);
        Assert.DoesNotContain("$", html);
    }

    [Fact]
    public void BlockQuote_GetHtml_CenterAlignment_MustNotEmitDollar()
    {
        //Act
        var html = new BlockQuote { Text = "q", Alignment = "center" }.GetHtml();

        //Assert
        Assert.Contains("margin: auto 0;", html);
        Assert.Contains(">q<", html);
        Assert.DoesNotContain("$", html);
    }

    [Fact]
    public void BlockCode_GetHtml_MustEscapePlainText()
    {
        //Act
        var html = new BlockCode { Code = "a < b & c" }.GetHtml();

        //Assert
        Assert.Contains("<code>a &lt; b &amp; c</code>", html);
        Assert.DoesNotContain("$", html);
    }

    [Fact]
    public void BlockTable_GetHtml_MustCloseTableAndUseTrTh()
    {
        //Act
        var html = new BlockTable
        {
            Content = [["h"], ["c"]],
            WithHeading = true,
            Rows = 2,
            Cols = 1,
        }.GetHtml();

        //Assert
        Assert.Contains("<tr>", html);
        Assert.Contains("</tr>", html);
        Assert.Contains("<th>h</th>", html);
        Assert.Contains("<td>c</td>", html);
        Assert.Contains("</table>", html);
    }

    [Fact]
    public void BlockTable_GetHtml_NoHeading_MustUseTdOnly()
    {
        //Act
        var html = new BlockTable { Content = [["c"]] }.GetHtml();

        //Assert
        Assert.Contains("<td>c</td>", html);
        Assert.DoesNotContain("<th>", html);
    }

    [Fact]
    public void RenderToHtml_ImportedHtml_MustRoundTripAllBlockTypes()
    {
        //Arrange
        var html = "<ul><li>a</li><li>b</li></ul>"
                   + "<blockquote>q</blockquote>"
                   + "<pre><code>x</code></pre>"
                   + "<table><tr><th>h</th></tr><tr><td>c</td></tr></table>";

        //Act
        var content = EditorJsContent.FromJsonAutoConvertToBlocks(html, out var isReplaced);
        var rendered = EditorTools.RenderToHtml(content);

        //Assert
        Assert.True(isReplaced);
        Assert.Contains("<li>a</li>", rendered);
        Assert.Contains("<li>b</li>", rendered);
        Assert.Contains("<blockquote class=\"editorjs block-quote\">q</blockquote>", rendered);
        Assert.Contains("<code>x</code>", rendered);
        Assert.Contains("<th>h</th>", rendered);
        Assert.Contains("<td>c</td>", rendered);
        Assert.Contains("</table>", rendered);
        Assert.DoesNotContain("$", rendered);
    }
}
