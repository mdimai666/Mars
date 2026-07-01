using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes.Common;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Implements.Test.Services;

namespace Mars.Nodes.Implements.Test.Nodes;

public class CallNodeTests : NodeServiceUnitTestBase
{
    string _callNodeName = "callNode-1";

    void Setup(TimeSpan? delay = null, TimeSpan? timeout = null)
    {
        var nodes = NodesWorkflowBuilder.Create()
                        .AddNext(new CallNode() { Name = _callNodeName, Timeout = timeout ?? TimeSpan.FromSeconds(2) })
                        .AddNext(new DelayNode() { DelayMillis = (int)(delay?.TotalMilliseconds ?? 200) })
                        .AddNext(TemplateNode.PlainTextVariant("OK-1"))
                        .AddNext(new CallResponseNode())
                        .BuildWithFlowNode();
        _nodeService.Deploy(nodes);
    }

    [Fact]
    public async Task Execute_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(CallNodeImpl.Execute);
        var input = new NodeMsg { Payload = "QQ" };
        Setup();

        //Act
        var result = await _nodeService.CallNode(_serviceProvider, _callNodeName, input.Payload, throwOnError: true);

        //Assert
        result.Ok.Should().BeTrue();
        result.Data.Should().Be("OK-1");
    }

    [Fact]
    public async Task Execute_NotExistNode_FailNotFound()
    {
        //Arrange
        _ = nameof(CallNodeImpl.Execute);
        var input = new NodeMsg { Payload = "QQ" };
        Setup();

        //Act
        var result = await _nodeService.CallNode(_serviceProvider, "_not_exist_id", input.Payload, throwOnError: true);

        //Assert
        result.Ok.Should().BeFalse();
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task Execute_TimeoutLimit_FailTimeout()
    {
        //Arrange
        _ = nameof(CallNodeImpl.Execute);
        var input = new NodeMsg { Payload = "QQ" };
        Setup(delay: TimeSpan.FromSeconds(4), timeout: TimeSpan.FromMilliseconds(200));

        //Act
        var action = () => _nodeService.CallNode(_serviceProvider, _callNodeName, input.Payload, throwOnError: true);

        //Assert
        await action.Should().ThrowAsync<TimeoutException>();
    }

}
