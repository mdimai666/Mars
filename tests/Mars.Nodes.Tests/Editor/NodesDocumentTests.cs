using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Front.Abstractions.Editor;
using Mars.Nodes.Front.Abstractions.Editor.Models;

namespace Mars.Nodes.Tests.Editor;

public class NodesDocumentTests
{
    [Fact]
    public void Recalculate_DerivesFlowsAndFlowNodes()
    {
        //Arrange
        var nodes = NodesWorkflowBuilder.Create().AddNext().AddNext().BuildWithFlowNode();
        var flow = (FlowNode)nodes[0];
        var doc = new NodesDocument();

        //Act
        doc.SetNodes(nodes.ToDictionary(s => s.Id));
        doc.Recalculate();
        var resolved = doc.TryResolveActiveFlow(null);

        //Assert
        resolved.Should().BeTrue();
        doc.Flows.Should().ContainSingle().Which.Should().BeSameAs(flow);
        doc.ActiveFlow.Should().BeSameAs(flow);
        doc.FlowNodes.Should().HaveCount(2);
        doc.FlowNodes.Keys.Should().BeEquivalentTo(nodes.Skip(1).Select(s => s.Id));
    }

    [Fact]
    public void SetNodes_SameInstance_ReturnsFalse()
    {
        //Arrange
        var doc = new NodesDocument();
        doc.SetNodes(new Dictionary<string, Node>()).Should().BeTrue();

        //Act & Assert
        doc.SetNodes(doc.Nodes).Should().BeFalse();
    }

    [Fact]
    public void AddNodesAndWires_AddsNodeAndConnectsWire()
    {
        //Arrange
        var nodes = NodesWorkflowBuilder.Create().AddNext().AddNext().BuildWithFlowNode();
        var doc = new NodesDocument();
        doc.SetNodes(nodes.ToDictionary(s => s.Id));
        doc.Recalculate();
        doc.TryResolveActiveFlow(null);

        var source = nodes[1];
        var extra = new TemplateNode { Container = nodes[0].Id };

        //Act
        doc.AddNodesAndWires([extra], [new NodeConnect(new NodeWire(source.Id), new NodeWire(extra.Id))]);
        doc.Recalculate();

        //Assert
        doc.Nodes.Should().ContainKey(extra.Id);
        source.Wires[0].Should().Contain(w => w.NodeId == extra.Id);
        doc.FlowNodes.Should().ContainKey(extra.Id);
    }

    [Fact]
    public void DeleteNodesAndWires_RemovesNodesAndDanglingWires()
    {
        //Arrange
        var nodes = NodesWorkflowBuilder.Create().AddNext().AddNext().AddNext().BuildWithFlowNode();
        var doc = new NodesDocument();
        doc.SetNodes(nodes.ToDictionary(s => s.Id));
        doc.Recalculate();
        doc.TryResolveActiveFlow(null);

        var middle = nodes[2];

        //Act
        doc.DeleteNodesAndWires([middle], []);
        doc.Recalculate();

        //Assert
        doc.Nodes.Should().NotContainKey(middle.Id);
        nodes[1].Wires.SelectMany(w => w).Should().NotContain(w => w.NodeId == middle.Id);
        doc.FlowNodes.Should().HaveCount(2);
    }

    [Fact]
    public void DeleteNodesAndWires_RemovesSpecificConnects()
    {
        //Arrange
        var nodes = NodesWorkflowBuilder.Create().AddNext().AddNext().BuildWithFlowNode();
        var doc = new NodesDocument();
        doc.SetNodes(nodes.ToDictionary(s => s.Id));
        doc.Recalculate();

        var source = nodes[1];
        var target = nodes[2];
        var connect = new NodeConnect(new NodeWire(source.Id), new NodeWire(target.Id));

        //Act
        doc.DeleteNodesAndWires([], [connect]);

        //Assert
        source.Wires.SelectMany(w => w).Should().NotContain(w => w.NodeId == target.Id);
        doc.Nodes.Should().ContainKey(target.Id);
    }

    [Fact]
    public void SaveNode_ReplacesInstanceAndRecalculatesFlowNodes()
    {
        //Arrange
        var nodes = NodesWorkflowBuilder.Create().AddNext().AddNext().BuildWithFlowNode();
        var doc = new NodesDocument();
        doc.SetNodes(nodes.ToDictionary(s => s.Id));
        doc.Recalculate();
        doc.TryResolveActiveFlow(null);

        var replacement = new TemplateNode { Id = nodes[1].Id, Container = nodes[0].Id, Name = "replaced" };

        //Act
        doc.SaveNode(replacement);

        //Assert
        doc.Nodes[nodes[1].Id].Should().BeSameAs(replacement);
        doc.FlowNodes[nodes[1].Id].Should().BeSameAs(replacement);
    }

    [Fact]
    public void TryResolveActiveFlow_PrefersRequestedId_AndRunsOnce()
    {
        //Arrange
        var flowA = new FlowNode { Order = 1 };
        var flowB = new FlowNode { Order = 2 };
        var doc = new NodesDocument();
        doc.SetNodes(new Dictionary<string, Node> { [flowA.Id] = flowA, [flowB.Id] = flowB });
        doc.Recalculate();

        //Act
        var first = doc.TryResolveActiveFlow(flowB.Id);
        var second = doc.TryResolveActiveFlow(flowA.Id);

        //Assert
        first.Should().BeTrue();
        doc.ActiveFlow.Should().BeSameAs(flowB);
        second.Should().BeFalse();
        doc.ActiveFlow.Should().BeSameAs(flowB);
    }

    [Fact]
    public void TryResolveActiveFlow_NoPreference_PicksFirstByOrder()
    {
        //Arrange
        var flowA = new FlowNode { Order = 2 };
        var flowB = new FlowNode { Order = 1 };
        var doc = new NodesDocument();
        doc.SetNodes(new Dictionary<string, Node> { [flowA.Id] = flowA, [flowB.Id] = flowB });
        doc.Recalculate();

        //Act
        doc.TryResolveActiveFlow(null);

        //Assert
        doc.ActiveFlow.Should().BeSameAs(flowB);
    }

    [Fact]
    public void ChangeFlow_SwitchesFlowNodes()
    {
        //Arrange
        var flowA = new FlowNode { Order = 1 };
        var flowB = new FlowNode { Order = 2 };
        var nodeA = new TemplateNode { Container = flowA.Id };
        var nodeB = new TemplateNode { Container = flowB.Id };
        var doc = new NodesDocument();
        doc.SetNodes(new Dictionary<string, Node>
        {
            [flowA.Id] = flowA,
            [flowB.Id] = flowB,
            [nodeA.Id] = nodeA,
            [nodeB.Id] = nodeB
        });
        doc.Recalculate();
        doc.TryResolveActiveFlow(null);
        doc.FlowNodes.Should().ContainKey(nodeA.Id).And.NotContainKey(nodeB.Id);

        //Act
        doc.ChangeFlow(flowB);

        //Assert
        doc.ActiveFlow.Should().BeSameAs(flowB);
        doc.FlowNodes.Should().ContainKey(nodeB.Id).And.NotContainKey(nodeA.Id);
    }

    [Fact]
    public void Recalculate_DerivesVarNodesAndLinkGraph()
    {
        //Arrange
        var varB = new VarNode { Name = "b" };
        var varA = new VarNode { Name = "a" };
        var linkOut = new LinkOutNode();
        var linkIn = new LinkInNode { OutLinksIds = [linkOut.Id] };
        var doc = new NodesDocument();

        //Act
        doc.SetNodes(new Dictionary<string, Node>
        {
            [varB.Id] = varB,
            [varA.Id] = varA,
            [linkOut.Id] = linkOut,
            [linkIn.Id] = linkIn
        });
        doc.Recalculate();

        //Assert
        doc.VarNodes.Select(v => v.Name).Should().ContainInOrder("a", "b");
        doc.InboundLinkOutNodes.Should().ContainKey(linkOut.Id);
        doc.InboundLinkOutNodes[linkOut.Id].Should().ContainSingle().Which.Should().BeSameAs(linkIn);
    }
}
