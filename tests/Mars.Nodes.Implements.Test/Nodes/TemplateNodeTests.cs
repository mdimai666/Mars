using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes.Functions;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Implements.Test.Services;
using Mars.TemplateEngine.Providers.HandlebarsProvider;

namespace Mars.Nodes.Implements.Test.Nodes;

public class TemplateNodeTests : NodeServiceUnitTestBase
{
    Task<NodeMsg> RenderTemplate(string templateText, object context, string propertyName = "Payload")
    {
        var input = new NodeMsg() { Payload = context };
        return RenderTemplate(input, templateText, propertyName);
    }

    Task<NodeMsg> RenderTemplate(NodeMsg input, string templateText, string propertyName = "Payload")
    {
        var node = new TemplateNode { Template = templateText, TemplateEngineId = HandlebarsTemplateEngine.Id, Property = propertyName };
        return ExecuteNode(node, input);
    }

    [Fact]
    public async Task Execute_RenderToText_Success()
    {
        //Arrange
        _ = nameof(TemplateNodeImpl.Execute);
        var templateText = "{{Payload.Name}}";
        var context = new { Name = "test" };

        //Act
        var msg = await RenderTemplate(templateText, context);

        //Assert
        msg.Payload.Should().BeEquivalentTo("test");
    }

    [Fact]
    public async Task Execute_SetResultToProperty_ShouldDidntChangePayload()
    {
        //Arrange
        _ = nameof(TemplateNodeImpl.Execute);
        var templateText = "{{SubData}}";
        var input = new NodeMsg { Payload = 123 };
        input.Set("SubData", "test");

        //Act
        var msg = await RenderTemplate(input, templateText, propertyName: "Sub1");

        //Assert
        msg.Context["Sub1"].Should().BeEquivalentTo("test");
        msg.Payload.Should().Be(123);
    }
}
