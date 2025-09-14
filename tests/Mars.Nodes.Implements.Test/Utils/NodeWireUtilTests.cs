using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Workspace;
using Mars.Nodes.Workspace.Components;

namespace Mars.Nodes.Implements.Test.Utils;

public class NodeWireUtilTests
{
    [Fact]
    public void GetLinkedNodes_BothSide_ShouldSuccess()
    {
        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                        .AddNext()
                                        .AddNext()
                                        .AddNext()
                                        .AddIndependent(new TemplateNode());

        //Act
        var linkedNodes = NodeWireUtil.GetLinkedNodes(builder.Nodes.ElementAt(1), builder.Nodes.ToDictionary(s => s.Id));

        //Assert
        builder.Nodes.Count().Should().Be(4);
        linkedNodes.Count().Should().Be(3);
        linkedNodes.Should().NotContainEquivalentOf(builder.LastGeneration());
    }

    [Fact]
    public void GetLinkedNodes_ShouldHandleCyclicGraph()
    {
        _ = nameof(NodeWireUtil.GetLinkedNodes);

        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                          .AddNext()
                                          .AddNext()
                                          .AddNext();

        var nodes = builder.Nodes.ToList();
        var all = nodes.ToDictionary(s => s.Id);

        // вручную замыкаем цикл: последний -> первый
        nodes.Last().Wires.Add([new NodeWire(nodes.First().Id)]);

        //Act
        var linked = NodeWireUtil.GetLinkedNodes(nodes.First(), all);

        //Assert
        linked.Count.Should().Be(3); // все три ноды, но без бесконечного цикла
        linked.Should().Contain(nodes[0]);
        linked.Should().Contain(nodes[1]);
        linked.Should().Contain(nodes[2]);
    }

    [Fact]
    public void GetOutputNodes_RetriveRootOutputChilds_ShouldSuccess()
    {
        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                        .AddNext()
                                        .AddNext()
                                        .AddNext(new FunctionNode(), new FunctionNode())
                                        .AddNext()
                                        .AddIndependent(new TemplateNode());

        //Act
        var outputNodes = NodeWireUtil.GetOutputNodes(builder.Nodes.ElementAt(1), builder.Nodes.ToDictionary(s => s.Id));

        //Assert
        builder.Nodes.Count().Should().Be(6);
        outputNodes.Count().Should().Be(2);
        outputNodes.Should().AllBeOfType<FunctionNode>();
    }

    [Fact]
    public void GetInputNodes_ShouldReturnParents()
    {
        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                          .AddNext()
                                          .AddNext()
                                          .AddNext();

        var target = builder.Nodes.ElementAt(2);

        //Act
        var inputNodes = NodeWireUtil.GetInputNodes(target, builder.Nodes.ToDictionary(s => s.Id));

        //Assert
        inputNodes.Should().ContainSingle();
        inputNodes.First().Should().Be(builder.Nodes.ElementAt(1));
    }

    [Fact]
    public void AreDirectlyConnected_ShouldReturnTrue_WhenConnected()
    {
        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                          .AddNext()
                                          .AddNext();

        var a = builder.Nodes.ElementAt(0);
        var b = builder.Nodes.ElementAt(1);

        //Act
        var result = NodeWireUtil.AreDirectlyConnected(a, b);

        //Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AreDirectlyConnected_ShouldReturnFalse_WhenNotConnected()
    {
        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                          .AddNext()
                                          .AddIndependent(new TemplateNode());

        var a = builder.Nodes.ElementAt(0);
        var b = builder.Nodes.ElementAt(1);

        //Act
        var result = NodeWireUtil.AreDirectlyConnected(a, b);

        //Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AreConnected_ShouldReturnTrue_WhenPathExists()
    {
        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                          .AddNext()
                                          .AddNext()
                                          .AddNext();

        var a = builder.Nodes.ElementAt(0);
        var c = builder.Nodes.ElementAt(2);

        //Act
        var result = NodeWireUtil.AreConnected(a, c, builder.Nodes.ToDictionary(s => s.Id));

        //Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AreConnected_ShouldReturnFalse_WhenNoPath()
    {
        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                          .AddNext()
                                          .AddIndependent(new TemplateNode());

        var a = builder.Nodes.ElementAt(0);
        var b = builder.Nodes.ElementAt(1);

        //Act
        var result = NodeWireUtil.AreConnected(a, b, builder.Nodes.ToDictionary(s => s.Id));

        //Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetLeafNodes_ShouldReturnNodesWithoutOutputs()
    {
        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                          .AddNext()
                                          .AddNext()
                                          .AddIndependent(new TemplateNode());

        //Act
        var leafNodes = NodeWireUtil.GetLeafNodes(builder.Nodes.ToDictionary(s => s.Id));

        //Assert
        leafNodes.Should().Contain(builder.LastGeneration());
        leafNodes.Should().Contain(n => n is TemplateNode);
    }

    [Fact]
    public void GetRootNodes_ShouldReturnNodesWithoutInputs()
    {
        _ = nameof(NodeWireUtil.GetRootNodes);

        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                          .AddNext()
                                          .AddNext()
                                          .AddIndependent(new TemplateNode());

        //Act
        var rootNodes = NodeWireUtil.GetRootNodes(builder.Nodes.ToDictionary(s => s.Id));

        //Assert
        rootNodes.Should().Contain(builder.Nodes.First());
        rootNodes.Should().Contain(n => n is TemplateNode);
    }

    [Fact]
    public void DrawWires_ShouldCreateCorrectCoordinates()
    {
        _ = nameof(NodeWireUtil.DrawWires);

        //Arrange
        var nodeA = new Node { Id = "A", X = 10, Y = 20, Wires = [] };
        var nodeB = new Node { Id = "B", X = 100, Y = 200, Wires = [] };
        var nodeC = new Node { Id = "C", X = 300, Y = 400, Wires = [] };

        // создаём провода
        nodeA.Wires.Add([new NodeWire("B", 0)]);   // A -> B (port 0)
        nodeB.Wires.Add([new NodeWire("C", 1)]);   // B -> C (port 1)

        var nodes = new Dictionary<string, Node>
        {
            [nodeA.Id] = nodeA,
            [nodeB.Id] = nodeB,
            [nodeC.Id] = nodeC
        };

        var nodeWirePointResolver = new NodeWirePointResolver();

        //Act
        var wires = NodeWireUtil.DrawWires(nodes, nodeWirePointResolver);

        //Assert
        wires.Should().HaveCount(2);

        // проверяем A -> B
        var wireAB = wires.First(wr => wr.Node1.NodeId == "A" && wr.Node2.NodeId == "B");
        wireAB.X1.Should().Be(nodeA.X + NodeComponent.CalcBodyWidth(nodeA) + 15f);
        wireAB.Y1.Should().Be(nodeA.Y + 23);
        wireAB.X2.Should().Be(nodeB.X + 8);
        wireAB.Y2.Should().Be(nodeB.Y + 23);

        // проверяем B -> C (port 1)
        var wireBC = wires.First(wr => wr.Node1.NodeId == "B" && wr.Node2.NodeId == "C");
        wireBC.X1.Should().Be(nodeB.X + NodeComponent.CalcBodyWidth(nodeB) + 15f);
        wireBC.Y1.Should().Be(nodeB.Y + 23);
        wireBC.X2.Should().Be(nodeC.X + 8);
        wireBC.Y2.Should().Be(nodeC.Y + 23);
    }
}
