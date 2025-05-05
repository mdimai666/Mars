using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Implements.Test.Services;
using FluentAssertions;

namespace Mars.Nodes.Implements.Test.Nodes;

public class TemplateNodeTests : NodeServiceUnitTestBase
{
    [Fact]
    public async Task Execute_TemplateCompileToText_Success()
    {
        //Arrange
        _ = nameof(TemplateNodeImpl.Execute);
        var input = new NodeMsg()
        {
            Payload = "123"
        };
        var template = "{{Payload}}";
        var node = new TemplateNode { Template = template };

        //Act
        var msg = await ExecuteNode(node, input);

        //Assert
        msg.Payload.Should().BeEquivalentTo("123");
    }
}
