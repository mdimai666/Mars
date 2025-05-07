using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Implements.Test.Services;

namespace Mars.Nodes.Implements.Test.Nodes;

public class HtmlParseNodeTests : NodeServiceUnitTestBase
{

    string Html => """
        <html>
        <body>
            <h1>Title</h1>
            <h2>Subtitle <span class="badge">2</span></h2>
            <div>
                <ul class="unsyled-list">
                    <li data-value="d-1">1</li>
                    <li data-value="d-2">2</li>
                    <li data-value="d-3">3</li>
                </ul>
            </div>
        </body>
        </html>
        """;

    [Fact]
    public async Task Execute_EmptyHtml_ShouldReturnEmptyArray()
    {
        //Arrange
        _ = nameof(HtmlParseNodeImpl.Execute);
        var input = new NodeMsg() { Payload = Html };
        var node = new HtmlParseNode { Selector = "not-exist-element" };

        //Act
        var msg = await ExecuteNode(node, input);

        //Assert
        msg.Payload.Should().BeEquivalentTo(Array.Empty<object>());
    }

    [Fact]
    public async Task Execute_TextSelector_ShouldReturnTextArray()
    {
        //Arrange
        _ = nameof(HtmlParseNodeImpl.Execute);
        var input = new NodeMsg() { Payload = Html };
        var node = new HtmlParseNode { Selector = "h2", Output = HtmlParseNodeOutput.Text };

        //Act
        var msg = await ExecuteNode(node, input);

        //Assert
        msg.Payload.Should().BeEquivalentTo((string[])["Subtitle 2"]);
    }

    [Fact]
    public async Task Execute_HtmlSelector_ShouldReturnHtmlArray()
    {
        //Arrange
        _ = nameof(HtmlParseNodeImpl.Execute);
        var input = new NodeMsg() { Payload = Html };
        var node = new HtmlParseNode { Selector = "h2", Output = HtmlParseNodeOutput.Html };

        //Act
        var msg = await ExecuteNode(node, input);

        //Assert
        msg.Payload.Should().BeEquivalentTo((string[])["Subtitle <span class=\"badge\">2</span>"]);
    }

    [Fact]
    public async Task Execute_MapToObjectsText_ShouldReturnDictionary()
    {
        //Arrange
        _ = nameof(HtmlParseNodeImpl.Execute);
        var input = new NodeMsg() { Payload = Html };
        var node = new HtmlParseNode
        {
            Selector = "ul li",
            Output = HtmlParseNodeOutput.MapToObjects,
            InputMappings = [
                new() { ReturnValue = InputMappingReturnValue.Text, Selector = "", OutputField = "f1" },
                new() { ReturnValue = InputMappingReturnValue.Attribute, Selector = "", OutputField = "attr", Attribute = "data-value" },
            ]
        };

        //Act
        var msg = await ExecuteNode(node, input);

        //Assert
        msg.Payload.Should().BeEquivalentTo((Dictionary<string, string?>[])
            [
                new (){ ["f1"] = "1", ["attr"] = "d-1" },
                new (){ ["f1"] = "2", ["attr"] = "d-2" },
                new (){ ["f1"] = "3", ["attr"] = "d-3" },
            ]);
    }
}
