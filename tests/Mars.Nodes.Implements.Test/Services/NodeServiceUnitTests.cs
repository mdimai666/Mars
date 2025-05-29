using System.Text.Json;
using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Converters;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Services;

namespace Mars.Nodes.Implements.Test.Services;

public class NodeServiceUnitTests : NodeServiceUnitTestBase
{
    [Fact]
    public void OrderNodesForInitialize_FirstFlowsAndConfigNodes_ShouldReturnSorted()
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
    public void DeserializeNodes_UnknownNodes_ShouldSuccessAsUnknownNodes()
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

        var json = JsonSerializer.Serialize(nodes);

        //Act
        var nodesFromJson = JsonSerializer.Deserialize<List<Node>>(json)!;

        //Assert
        var unknownNode = nodesFromJson.ElementAt(2);
        unknownNode.GetType().Should().Be(typeof(UnknownNode));
        ((UnknownNode)unknownNode).JsonBody.Should().Contain("123");
    }

    [Fact]
    public void SerializeNodes_UnknownNodes_ShouldSaveInitialNodeBody()
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

        var json = JsonSerializer.Serialize(nodes);
        var nodesFromJson = JsonSerializer.Deserialize<List<Node>>(json)!;

        //Act
        var serializedWithUnknownNodes = JsonSerializer.Serialize(nodesFromJson)!;

        //Assert
        serializedWithUnknownNodes.Should().BeEquivalentTo(json);
    }

    class SomeNonExistNode : Node
    {
        public string ImportantData { get; set; } = "";
    }

    [Fact]
    public void ReplaceDefaultFieldsToEmptyString_DefaultValuesMustEmpty_ShouldReturnEmptyFields()
    {
        var nodes = new List<Node>
        {
            new InjectNode(){ },
            new InjectNode(){ Color = "red", Icon = "/new/icon/icon-48.png" },
        };
        var newNodes = _nodeService.ReplaceDefaultFieldsToEmptyString(nodes).ToArray();

        newNodes[0].Color.Should().BeEmpty();
        newNodes[0].Icon.Should().BeEmpty();

        newNodes[1].Color.Should().Be("red");
        newNodes[1].Icon.Should().Be("/new/icon/icon-48.png");
    }
}
