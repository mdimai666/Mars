using System.Text.Json;
using EditorJsBlazored.Blocks;
using EditorJsBlazored.Core;

namespace Test.EditorJsBlazored;

public class EditorToolsRenderTests
{
    [Fact]
    public void RenderToHtml_OneParagraph_Success()
    {
        //Arrange
        var block = new BlockParagraph { Text = "123" };
        var content = new
        {
            blocks = (object[])[
                new { type = "paragraph", data = block }
            ]
        };

        var json = JsonSerializer.Serialize(content, JsonSerializerOptions.Web);
        var expect = "<p>123</p>";

        //Act
        var html = EditorTools.RenderToHtml(json);

        //Assert
        Assert.Contains(expect, html);
    }
}
