using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes.Common;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Tests.NodesForTesting;
using Mars.Nodes.Tests.Services;

namespace Mars.Nodes.Tests.Nodes;

public class LinkInNodeTests : NodeServiceUnitTestBase
{
    [Fact]
    public async Task Execute_LinkInNodeTriggersMultipleLinkOutNodes_AllCallbacksExecuted()
    {
        //Arrange
        _ = nameof(LinkInNodeImpl.Execute);
        var input = new NodeMsg() { Payload = 123 };
        var signals = new HashSet<string>();
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var outNode1 = Guid.NewGuid().ToString();
        var outNode2 = Guid.NewGuid().ToString();

        string[] expectSignals = ["1", "2", "3"];

        void OnSignal(string signal)
        {
            lock (signals)
            {
                signals.Add(signal);
                if (signals.Count == expectSignals.Length) tcs.TrySetResult();
            }
        }

        var builder = NodesWorkflowBuilder.Create().AddNext(
                        NodesWorkflowBuilder.Create()
                            .AddNext(new InjectNode())
                            .AddNext(new LinkInNode() { OutLinksIds = [outNode1, outNode2] }),
                        NodesWorkflowBuilder.Create()
                            .AddNext(new LinkOutNode() { Id = outNode1 })
                            .AddNext(new TestCallBackNode() { Callback = (_, _) => OnSignal("1") },
                                        new TestCallBackNode() { Callback = (_, _) => OnSignal("2") }),
                        NodesWorkflowBuilder.Create()
                            .AddNext(new LinkOutNode() { Id = outNode2 })
                            .AddNext(new TestCallBackNode() { Callback = (_, _) => OnSignal("3") })
                        );

        //Act
        var msg = await RunUsingTaskManager(builder);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        //Assert
        signals.Should().BeEquivalentTo(expectSignals);
    }
}
