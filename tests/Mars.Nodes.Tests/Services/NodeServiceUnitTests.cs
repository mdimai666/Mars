using System.Text.Json;
using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Converters;
using Mars.Nodes.Core.Locators;
using Mars.Nodes.Host.Services;

namespace Mars.Nodes.Tests.Services;

public class NodeServiceUnitTests : NodeServiceUnitTestBase
{
    [Fact]
    public void OrderNodesForInitialize_FirstFlowsAndConfigNodes_ReturnsSorted()
    {
        //Arrange
        _ = nameof(NodeService.OrderNodesForInitialize);
        var nodes = new List<Node>
        {
            new InjectNode(),
            new DebugNode(),
            new FlowNode(),
        };

        //Act
        var orderedNodes = NodeService.OrderNodesForInitialize(nodes);

        //Assert
        orderedNodes[0].GetType().Should().Be(typeof(FlowNode));
    }

    [Fact]
    public void DeserializeNodes_UnknownNodes_SucceedsAsUnknownNodes()
    {
        //Arrange
        _ = nameof(NodeJsonConverter);
        var flowNode = new FlowNode();
        var nodes = new List<Node>
        {
            flowNode,
            new InjectNode(){ Container = flowNode.Id },
            new SomeNonExistNode(){ Container = flowNode.Id, ImportantData = "123" },
        };

        var json = JsonSerializer.Serialize(nodes, _jsonSerializerOptions);

        var nodesLocator__WithoutSomeNonExistNode = new NodesLocator();
        nodesLocator__WithoutSomeNonExistNode.RegisterAssembly(typeof(InjectNode).Assembly);

        var jsonSerializerOptions2 = nodesLocator__WithoutSomeNonExistNode.CreateJsonSerializerOptions();

        //Act
        var nodesFromJson = JsonSerializer.Deserialize<List<Node>>(json, jsonSerializerOptions2)!;

        //Assert
        var unknownNode = nodesFromJson.ElementAt(2);
        unknownNode.GetType().Should().Be(typeof(UnknownNode));
        ((UnknownNode)unknownNode).JsonBody.Should().Contain("123");
    }

    [Fact]
    public void SerializeNodes_UnknownNodes_SavesInitialNodeBody()
    {
        //Arrange
        _ = nameof(NodeJsonConverter);
        var flowNode = new FlowNode();
        var nodes = new List<Node>
        {
            flowNode,
            new InjectNode(){ Container = flowNode.Id },
            new SomeNonExistNode(){ Container = flowNode.Id, ImportantData = "123" },
        };

        var json = JsonSerializer.Serialize(nodes, _jsonSerializerOptions);
        var nodesFromJson = JsonSerializer.Deserialize<List<Node>>(json, _jsonSerializerOptions)!;

        //Act
        var serializedWithUnknownNodes = JsonSerializer.Serialize(nodesFromJson, _jsonSerializerOptions)!;

        //Assert
        serializedWithUnknownNodes.Should().BeEquivalentTo(json);
    }

    class SomeNonExistNode : Node
    {
        public string ImportantData { get; set; } = "";
    }

    [Fact]
    public void ReplaceDefaultFieldsToEmptyString_DefaultValuesMustEmpty_ReturnsEmptyFields()
    {
        //Arrange
        _ = nameof(NodeService.ReplaceDefaultFieldsToEmptyString);
        var nodes = new List<Node>
        {
            new InjectNode(){ },
            new InjectNode(){ Color = "red", Icon = "/new/icon/icon-48.png" },
        };

        //Act
        var newNodes = _nodeService.ReplaceDefaultFieldsToEmptyString(nodes).ToArray();

        //Assert
        newNodes[0].Color.Should().BeEmpty();
        newNodes[0].Icon.Should().BeEmpty();

        newNodes[1].Color.Should().Be("red");
        newNodes[1].Icon.Should().Be("/new/icon/icon-48.png");
    }

    [Fact]
    public void ReplaceDefaultFieldsToEmptyString_DifferentOutputCount_DoesNotThrowError()
    {
        //Arrange
        _ = nameof(NodeService.ReplaceDefaultFieldsToEmptyString);
        var nodes = new List<Node>
        {
            new FunctionNode(){ Outputs = [new(), new()], Inputs = [new(), new()] },
        };

        //Act
        var action = () => _nodeService.ReplaceDefaultFieldsToEmptyString(nodes).ToArray();

        //Assert
        action.Should().NotThrow();
    }
}
