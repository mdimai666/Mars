using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Front.Abstractions.Editor.Models;

namespace Mars.Nodes.Tests.Utils;

public class DebugSnapshotScopeUtilTests
{
    [Fact]
    public void BuildUpstreamScope_LinearChain_ReturnsStartAndAllUpstream()
    {
        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                          .AddNext()
                                          .AddNext()
                                          .AddNext();
        var nodes = builder.Nodes.ToList();

        //Act
        var scope = DebugSnapshotScopeUtil.BuildUpstreamScope(nodes, nodes[2].Id);

        //Assert
        scope.Should().BeEquivalentTo([nodes[0].Id, nodes[1].Id, nodes[2].Id]);
    }

    [Fact]
    public void BuildUpstreamScope_DoesNotIncludeDownstream()
    {
        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                          .AddNext()
                                          .AddNext()
                                          .AddNext();
        var nodes = builder.Nodes.ToList();

        //Act
        var scope = DebugSnapshotScopeUtil.BuildUpstreamScope(nodes, nodes[1].Id);

        //Assert
        scope.Should().BeEquivalentTo([nodes[0].Id, nodes[1].Id]);
    }

    [Fact]
    public void BuildUpstreamScope_BranchingUpstream_IncludesAllSources()
    {
        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                          .AddNext()
                                          .AddNext(new FunctionNode(), new FunctionNode())
                                          .AddNext();
        var nodes = builder.Nodes.ToList();

        //Act
        var scope = DebugSnapshotScopeUtil.BuildUpstreamScope(nodes, nodes[3].Id);

        //Assert
        nodes.Should().HaveCount(4);
        scope.Should().BeEquivalentTo(nodes.Select(s => s.Id));
    }

    [Fact]
    public void BuildUpstreamScope_CyclicGraph_Terminates()
    {
        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                          .AddNext()
                                          .AddNext()
                                          .AddNext();
        var nodes = builder.Nodes.ToList();

        // вручную замыкаем цикл: последний -> первый
        nodes[2].Wires.Add([new NodeWire(nodes[0].Id)]);

        //Act
        var scope = DebugSnapshotScopeUtil.BuildUpstreamScope(nodes, nodes[0].Id);

        //Assert
        scope.Should().BeEquivalentTo(nodes.Select(s => s.Id));
    }

    [Fact]
    public void BuildUpstreamScope_NullStartId_ReturnsEmpty()
    {
        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                          .AddNext()
                                          .AddNext();
        var nodes = builder.Nodes.ToList();

        //Act
        var scope = DebugSnapshotScopeUtil.BuildUpstreamScope(nodes, null);

        //Assert
        scope.Should().BeEmpty();
    }

    [Fact]
    public void BuildUpstreamScope_MultipleStartIds_MergesScopes()
    {
        //Arrange
        var builder = NodesWorkflowBuilder.Create()
                                          .AddNext()
                                          .AddNext()
                                          .AddNext()
                                          .AddIndependent(new TemplateNode());
        var nodes = builder.Nodes.ToList();

        //Act
        var scope = DebugSnapshotScopeUtil.BuildUpstreamScope(nodes, nodes[1].Id, nodes[3].Id);

        //Assert
        scope.Should().BeEquivalentTo([nodes[0].Id, nodes[1].Id, nodes[3].Id]);
    }
}
