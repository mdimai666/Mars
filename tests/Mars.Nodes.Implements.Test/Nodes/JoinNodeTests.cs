using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes.Sequences;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Core.Nodes.Sequences;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Implements.Test.NodesForTesting;
using Mars.Nodes.Implements.Test.Services;

namespace Mars.Nodes.Implements.Test.Nodes;

public class JoinNodeTests : NodeServiceUnitTestBase
{
    void SetupNodes(NodesWorkflowBuilder builder, Action<object> callback)
    {
        var callbackNode = new TestCallBackNode() { Callback = (input, _) => callback(input.Payload!) };
        var nodes = NodesWorkflowBuilder.Create()
                .AddNext(builder)
                .AddNext([callbackNode], catchAllWires: true)
                .BuildWithFlowNode();
        _nodeService.Deploy(nodes);
    }

    void SetupNodes(NodesWorkflowBuilder builder, Action<object, ExecutionParameters> callback)
    {
        var callbackNode = new TestCallBackNode() { Callback = (input, p) => callback(input.Payload!, p) };
        var nodes = NodesWorkflowBuilder.Create()
                .AddNext(builder)
                .AddNext([callbackNode], catchAllWires: true)
                .BuildWithFlowNode();
        _nodeService.Deploy(nodes);
    }

    void Inject(object payload, int injectPortIndex = 0)
    {
        var nodes = _nodeService.BaseNodes.Values;
        var injectNode = nodes.First(node => node is not FlowNode);
        var input = new NodeMsg() { Payload = payload };
        _ = _nodeTaskManager.CreateJob(_serviceProvider, injectNode.Id, input, injectPortIndex: injectPortIndex, throwOnError: true);
    }

    [Fact]
    public async Task ExecuteForCountAggregation_ValidCount_ShouldOnePack()
    {
        //Arrange
        _ = nameof(JoinNodeImpl.ExecuteForCountAggregation);
        List<object> messages = [];

        SetupNodes(NodesWorkflowBuilder.Create()
                        .AddNext(new JoinNode() { Mode = JoinNode.JoinMode.CountAggregation, MessageCount = 5 }),
                    callback);

        //Act
        foreach (var i in Enumerable.Range(1, 5))
        {
            Inject(i);
        }
        await Task.Delay(10);

        void callback(object payload)
        {
            messages.Add(payload);
        }

        //Assert
        messages.Should().HaveCount(1);
        messages[0].Should().BeEquivalentTo(Enumerable.Range(1, 5));
    }

    [Fact]
    public async Task ExecuteForCountAggregation_CountBelowMinimum_ShouldBeEmpty()
    {
        //Arrange
        _ = nameof(JoinNodeImpl.ExecuteForCountAggregation);
        List<object> messages = [];

        SetupNodes(NodesWorkflowBuilder.Create()
                        .AddNext(new JoinNode() { Mode = JoinNode.JoinMode.CountAggregation, MessageCount = 5 }),
                    callback);

        //Act
        foreach (var i in Enumerable.Range(1, 4))
        {
            Inject(i);
        }

        void callback(object payload)
        {
            messages.Add(payload);
        }

        //Assert
        await Task.Delay(100);
        messages.Should().HaveCount(0);
    }

    [Fact]
    public async Task ExecuteForTimeAggregation_TimeElapsed_ShouldReturnMessagePack()
    {
        //Arrange
        _ = nameof(JoinNodeImpl.ExecuteForTimeAggregation);
        List<object> messages = [];
        var expectMessagesInPack = 4;
        var expectMessages = 1;
        var delayTimeBetweenPack = 500;
        var delayTimeSec = expectMessagesInPack * delayTimeBetweenPack / 1000;

        SetupNodes(NodesWorkflowBuilder.Create()
                        .AddNext(new JoinNode() { Mode = JoinNode.JoinMode.TimeAggregation, AggregationTimeSeconds = delayTimeSec }),
                    callback);

        //Act
        foreach (var i in Enumerable.Range(1, expectMessagesInPack + 2))
        {
            Inject(i);
            await Task.Delay(delayTimeBetweenPack);
        }

        void callback(object payload)
        {
            messages.Add(payload);
        }

        //Assert

        messages.Should().HaveCount(expectMessages);
        messages[0].Should().BeEquivalentTo(Enumerable.Range(1, expectMessagesInPack));
    }

    public NodesWorkflowBuilder SetupInputAggregationBuilder(JoinNode joinNode)
    {
        var builder = NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode())
            .AddNext(
                    NodesWorkflowBuilder.Create()
                        .AddNext(new TemplateNode() { Name = "input 1", Template = "input 1" }),
                    NodesWorkflowBuilder.Create()
                        .AddNext(new DelayNode() { DelayMillis = 300 })
                        .AddNext(new TemplateNode() { Name = "input 2", Template = "input 2" }),
                    NodesWorkflowBuilder.Create()
                        .AddNext(new DelayNode() { DelayMillis = 100 })
                        .AddNext(new TemplateNode() { Name = "input 3", Template = "input 3" })
                    )
            .AddNext([joinNode], catchAllWires: true);

        var templates = builder.Nodes.Where(node => node is TemplateNode).ToList();
        for (int port = 0; port < templates.Count; port++)
        {
            var templateNode = templates[port];
            templateNode.Wires = [[new(joinNode.Id, port)]];
        }

        return builder;
    }

    [Fact]
    public async Task ExecuteForInputAggregation_AllInputsReceived_ShouldReturnAggregatedMessage()
    {
        //Arrange
        _ = nameof(JoinNodeImpl.ExecuteForInputAggregation);
        List<object> messages = [];
        var joinNode = new JoinNode()
        {
            Mode = JoinNode.JoinMode.InputAggregation,
            Inputs = [new(), new(), new()],
            InputAggregationTimeoutSeconds = 15
        };
        string[] expectData = ["input 1", "input 2", "input 3"];

        SetupNodes(SetupInputAggregationBuilder(joinNode), callback);

        //Act
        Inject("x");

        void callback(object payload)
        {
            messages.Add(payload);
        }

        //Assert
        await Task.Delay(350);
        messages.Should().HaveCount(1);
        //порядок важен. По индексу портов.
        messages[0].Should().BeEquivalentTo(expectData);
    }

    [Fact]
    public async Task ExecuteForInputAggregation_NotAllInputsReceived_ShouldNotNextReturn()
    {
        //Arrange
        _ = nameof(JoinNodeImpl.ExecuteForInputAggregation);
        List<object> messages = [];
        var joinNode = new JoinNode()
        {
            Mode = JoinNode.JoinMode.InputAggregation,
            Inputs = [new(), new(), new()],
            InputAggregationTimeoutSeconds = 15
        };
        string[] expectData = ["input 1", "input 2"];

        var builder = SetupInputAggregationBuilder(joinNode);
        builder.Nodes.Last(s => s is TemplateNode).Wires = []; //unlink last port

        SetupNodes(builder, callback);

        //Act
        Inject("x");

        void callback(object payload)
        {
            messages.Add(payload);
        }

        //Assert
        messages.Should().HaveCount(0);
    }

    [Fact]
    public async Task ExecuteForInputAggregation_WhenTimeoutExpiresNotAllInputsReceived_ShouldTimeoutAndReturnAggregated()
    {
        //Arrange
        _ = nameof(JoinNodeImpl.ExecuteForInputAggregation);
        List<object> messages = [];
        int? lastTriggedPort = null;
        var joinNode = new JoinNode()
        {
            Mode = JoinNode.JoinMode.InputAggregation,
            Inputs = [new(), new(), new()],
            InputAggregationTimeoutSeconds = 1
        };
        string[] expectData = ["input 1", "input 2"];

        var builder = SetupInputAggregationBuilder(joinNode);
        builder.Nodes.Last(s => s is TemplateNode).Wires = []; //unlink last port

        SetupNodes(builder, callback);

        //Act
        Inject("x");

        void callback(object payload, ExecutionParameters executionParameters)
        {
            lastTriggedPort = executionParameters.SourceOutputPort;
            messages.Add(payload);
        }

        //Assert
        await Task.Delay(1500);
        messages.Should().HaveCount(1);
        lastTriggedPort.Should().Be(1);
        messages[0].Should().BeEquivalentTo(expectData);
    }
}
