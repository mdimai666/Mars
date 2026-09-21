using FluentAssertions;
using Mars.Nodes.Abstractions.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Host.Services;

namespace Mars.Nodes.Tests.DebugMode;

public class NodeDebugSnapshotBuilderTests
{
    [Fact]
    public void Build_LongString_IsCut()
    {
        var msg = new NodeMsg { Payload = new string('a', 500) };

        var snapshot = NodeDebugSnapshotBuilder.Build(msg, "node1", 0);

        snapshot.NodeId.Should().Be("node1");
        snapshot.Port.Should().Be(0);
        snapshot.Json.Should().Contain("...");
        snapshot.Json.Length.Should().BeLessThan(500);
    }

    [Fact]
    public void Build_MessageShape_KeepsPayloadAndContextKeysAtRoot()
    {
        var msg = new NodeMsg { Payload = "hello" };
        msg.Set("count", 42);

        var snapshot = NodeDebugSnapshotBuilder.Build(msg, "node1", 2);

        snapshot.Json.Should().Contain("\"Payload\":\"hello\"");
        snapshot.Json.Should().Contain("\"count\":42");
    }

    [Fact]
    public void Build_ManyItems_CapsTheCollection()
    {
        var msg = new NodeMsg { Payload = Enumerable.Range(0, 500).ToArray() };

        var snapshot = NodeDebugSnapshotBuilder.Build(msg, "node1", 0);

        snapshot.Json.Split(',').Length.Should().BeLessThan(NodeDebugSnapshotBuilder.DefaultMaxItems + 5);
    }

    [Fact]
    public void Build_DeepStructure_StopsAtMaxDepth()
    {
        var msg = new NodeMsg { Payload = new Level1 { Next = new Level2 { Next = new Level3 { Next = new Level4 { Value = "deep" } } } } };

        var snapshot = NodeDebugSnapshotBuilder.Build(msg, "node1", 0);

        // На пределе глубины объект остаётся виден, но уже как обрезанная строка, а не как структура.
        snapshot.Json.Should().Contain("\"Next\":\"");
        snapshot.Json.Length.Should().BeLessThan(400);
    }

    [Fact]
    public void Build_CyclicValue_Terminates()
    {
        var loop = new Loop();
        loop.Self = loop;
        var msg = new NodeMsg { Payload = loop };

        var snapshot = NodeDebugSnapshotBuilder.Build(msg, "node1", 0);

        snapshot.Json.Should().Contain("\"Self\"");
        snapshot.Json.Length.Should().BeLessThan(300);
    }

    private sealed class Level1 { public Level2? Next { get; set; } }
    private sealed class Level2 { public Level3? Next { get; set; } }
    private sealed class Level3 { public Level4? Next { get; set; } }
    private sealed class Level4 { public string Value { get; set; } = ""; }

    private sealed class Loop { public Loop? Self { get; set; } }
}

public class NodeDebugStoreTests
{
    static readonly TimeSpan WaitForThrottle = TimeSpan.FromMilliseconds(400);

    [Fact]
    public async Task Save_WhenDebugModeOff_StoresNothing()
    {
        var store = new NodeDebugStore(new DebugModeState { Enabled = false });

        store.Save(new NodeMsg { Payload = "hello" }, "node1", 0);
        await Task.Delay(WaitForThrottle);

        store.Get(["node1"]).Should().BeEmpty();
    }

    [Fact]
    public async Task Save_WhenDebugModeOn_StoresSnapshotOfSourcePort()
    {
        var store = new NodeDebugStore(new DebugModeState { Enabled = true });

        store.Save(new NodeMsg { Payload = "hello" }, "node1", 3);
        await Task.Delay(WaitForThrottle);

        var snapshots = store.Get(["node1"]);
        snapshots.Should().ContainKey("node1");
        snapshots["node1"].Should().ContainSingle()
            .Which.Should().Match<NodeDebugSnapshot>(s => s.Port == 3 && s.Json.Contains("hello"));
    }

    [Fact]
    public async Task Save_DifferentPortsOfOneNode_AreKeptSeparately()
    {
        var store = new NodeDebugStore(new DebugModeState { Enabled = true });

        store.Save(new NodeMsg { Payload = "first" }, "node1", 0);
        store.Save(new NodeMsg { Payload = "second" }, "node1", 1);
        await Task.Delay(WaitForThrottle);

        store.Get(["node1"])["node1"].Select(s => s.Port).Should().BeEquivalentTo([0, 1]);
    }

    [Fact]
    public async Task Save_FastSequence_KeepsTheLastValue()
    {
        var store = new NodeDebugStore(new DebugModeState { Enabled = true });

        store.Save(new NodeMsg { Payload = "first" }, "node1", 0);
        store.Save(new NodeMsg { Payload = "second" }, "node1", 0);
        await Task.Delay(WaitForThrottle);

        store.Get(["node1"])["node1"].Should().ContainSingle().Which.Json.Should().Contain("second");
    }

    [Fact]
    public async Task Save_WithoutSourceNode_IsIgnored()
    {
        var store = new NodeDebugStore(new DebugModeState { Enabled = true });

        store.Save(new NodeMsg { Payload = "hello" }, "", 0);
        await Task.Delay(WaitForThrottle);

        store.Get([""]).Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ReturnsOnlyRequestedNodes()
    {
        var store = new NodeDebugStore(new DebugModeState { Enabled = true });

        store.Save(new NodeMsg { Payload = "one" }, "node1", 0);
        store.Save(new NodeMsg { Payload = "two" }, "node2", 0);
        await Task.Delay(WaitForThrottle);

        var snapshots = store.Get(["node1"]);

        snapshots.Should().ContainKey("node1");
        snapshots.Should().NotContainKey("node2");
    }

    [Fact]
    public async Task Get_ExpiredSnapshot_IsDropped()
    {
        var store = new NodeDebugStore(new DebugModeState { Enabled = true });

        store.Save(new NodeMsg { Payload = "hello" }, "node1", 0);
        await Task.Delay(WaitForThrottle);

        store.Clock = () => DateTime.Now + NodeDebugStore.Ttl + TimeSpan.FromMinutes(1);

        store.Get(["node1"]).Should().BeEmpty();
    }

    [Fact]
    public async Task Get_AfterExpiryWindow_GoneForEveryNode()
    {
        var store = new NodeDebugStore(new DebugModeState { Enabled = true });

        store.Save(new NodeMsg { Payload = "hello" }, "node1", 0);
        await Task.Delay(WaitForThrottle);

        store.Clock = () => DateTime.Now + NodeDebugStore.Ttl - TimeSpan.FromMinutes(1);
        store.Get(["node1"]).Should().ContainKey("node1");

        store.Clock = () => DateTime.Now + NodeDebugStore.Ttl + TimeSpan.FromMinutes(1);
        store.Get(["node1"]).Should().BeEmpty();
    }
}

public class DebugModeStateTests
{
    [Fact]
    public void Enabled_DefaultsToFalse()
    {
        new DebugModeState().Enabled.Should().BeFalse();
    }

    [Fact]
    public void Enabled_IsNotPersisted()
    {
        var mode = new DebugModeState { Enabled = true };

        ((INodeDebugMode)mode).Enabled.Should().BeTrue();
        new DebugModeState().Enabled.Should().BeFalse();
    }
}
