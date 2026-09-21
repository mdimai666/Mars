using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Tests.Services;

namespace Mars.Nodes.Tests.DebugMode;

[Collection("TimingSensitive")]
public class NodeDebugCaptureTests : NodeServiceUnitTestBase
{
    [Fact]
    public async Task DebugModeOn_CapturesMessageTheNodeSentFurther()
    {
        DebugMode.Enabled = true;
        var inject = new InjectNode
        {
            Fields =
            [
                new() { Key = "Payload", Value = "hello" },
                new() { Key = "count", VarType = "int", Value = "42" },
            ]
        };

        await RunUsingTaskManager(inject);

        var snapshots = DebugStore.Get([inject.Id]);

        snapshots.Should().ContainKey(inject.Id);
        snapshots[inject.Id].Should().ContainSingle()
            .Which.Should().Match<NodeDebugSnapshot>(s => s.Port == 0
                && s.Json.Contains("\"Payload\":\"hello\"")
                && s.Json.Contains("\"count\":42"));
    }

    [Fact]
    public async Task DebugModeOff_StoresNothing()
    {
        var inject = new InjectNode { Fields = [new() { Key = "Payload", Value = "hello" }] };

        await RunUsingTaskManager(inject);

        DebugStore.Get([inject.Id]).Should().BeEmpty();
    }

    [Fact]
    public async Task DebugModeOn_CapturesEveryNodeOfTheChain()
    {
        DebugMode.Enabled = true;
        var first = new InjectNode { Fields = [new() { Key = "Payload", Value = "one" }] };
        var second = new EvalNode { Input = "msg.Payload + 1" };

        await RunUsingTaskManager(NodesWorkflowBuilder.Create().AddNext(first).AddNext(second));

        var snapshots = DebugStore.Get([first.Id, second.Id]);

        snapshots.Should().ContainKey(first.Id);
        snapshots.Should().ContainKey(second.Id);
    }

    [Fact]
    public async Task DebugModeOn_CapturedSnapshotHasFlatValues()
    {
        DebugMode.Enabled = true;
        var inject = new InjectNode { Fields = [new() { Key = "Payload", Value = "hello" }] };

        await RunUsingTaskManager(inject);

        DebugStore.Get([inject.Id])[inject.Id].Single()
            .Values.Should().Contain("Payload", "hello");
    }
}
