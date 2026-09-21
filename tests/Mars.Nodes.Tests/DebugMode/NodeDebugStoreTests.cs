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

    [Fact]
    public void Build_Values_FlattensNestedPathsAndArrayIndexes()
    {
        var msg = new NodeMsg
        {
            Payload = new Dictionary<string, object?>
            {
                ["user"] = new Dictionary<string, object?> { ["email"] = "a@b.c" },
                ["items"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["name"] = "one" },
                    new Dictionary<string, object?> { ["name"] = "two" },
                },
            }
        };
        msg.Set("count", 42);

        var values = NodeDebugSnapshotBuilder.Build(msg, "node1", 0).Values!;

        values.Should().Contain("Payload.user.email", "a@b.c");
        values.Should().Contain("Payload.items[0].name", "one");
        values.Should().Contain("Payload.items[1].name", "two");
        values.Should().Contain("Payload.items", "[2 items]");
        values.Should().Contain("count", "42");
    }

    [Fact]
    public void Build_Values_UseInvariantCultureForNumbers()
    {
        var msg = new NodeMsg { Payload = 1.5 };

        var values = NodeDebugSnapshotBuilder.Build(msg, "node1", 0).Values!;

        values.Should().Contain("Payload", "1.5");
    }

    private sealed class Level1 { public Level2? Next { get; set; } }
    private sealed class Level2 { public Level3? Next { get; set; } }
    private sealed class Level3 { public Level4? Next { get; set; } }
    private sealed class Level4 { public string Value { get; set; } = ""; }

    private sealed class Loop { public Loop? Self { get; set; } }
}

public class NodeDebugStoreTests
{
    static NodeDebugStore CreateStore(out DebugModeState mode)
    {
        mode = new DebugModeState { Enabled = true };
        return new NodeDebugStore(mode);
    }

    [Fact]
    public void Save_WhenDebugModeOff_StoresNothing()
    {
        var store = new NodeDebugStore(new DebugModeState { Enabled = false });

        store.Save(new NodeMsg { Payload = "hello" }, "node1", 0).Should().BeFalse();

        store.Get(["node1"]).Should().BeEmpty();
    }

    [Fact]
    public void Save_WhenDebugModeOn_StoresSnapshotOfSourcePortImmediately()
    {
        var store = CreateStore(out _);

        store.Save(new NodeMsg { Payload = "hello" }, "node1", 3).Should().BeTrue();

        var snapshots = store.Get(["node1"]);
        snapshots.Should().ContainKey("node1");
        snapshots["node1"].Should().ContainSingle()
            .Which.Should().Match<NodeDebugSnapshot>(s => s.Port == 3 && s.Json.Contains("hello"));
    }

    [Fact]
    public void Save_SnapshotIsBuiltSynchronously_LaterPayloadMutationDoesNotLeak()
    {
        var store = CreateStore(out _);
        var payload = new Mutable { Value = "before" };
        var msg = new NodeMsg { Payload = payload };

        store.Save(msg, "node1", 0);
        payload.Value = "after";

        store.Get(["node1"])["node1"].Single().Json.Should().Contain("before").And.NotContain("after");
    }

    [Fact]
    public void Save_DifferentPortsOfOneNode_AreKeptSeparately()
    {
        var store = CreateStore(out _);

        store.Save(new NodeMsg { Payload = "first" }, "node1", 0);
        store.Save(new NodeMsg { Payload = "second" }, "node1", 1);

        store.Get(["node1"])["node1"].Select(s => s.Port).Should().BeEquivalentTo([0, 1]);
    }

    [Fact]
    public void Save_InsideThrottleWindow_IsSkipped()
    {
        var store = CreateStore(out _);
        var now = DateTime.UtcNow;
        store.Clock = () => now;

        store.Save(new NodeMsg { Payload = "first" }, "node1", 0).Should().BeTrue();
        store.Save(new NodeMsg { Payload = "second" }, "node1", 0).Should().BeFalse();

        store.Get(["node1"])["node1"].Should().ContainSingle().Which.Json.Should().Contain("first");
    }

    [Fact]
    public void Save_AfterThrottleWindow_ReplacesSnapshot()
    {
        var store = CreateStore(out _);
        var now = DateTime.UtcNow;
        store.Clock = () => now;

        store.Save(new NodeMsg { Payload = "first" }, "node1", 0).Should().BeTrue();

        now += NodeDebugStore.ThrottleDelay + TimeSpan.FromMilliseconds(1);
        store.Save(new NodeMsg { Payload = "second" }, "node1", 0).Should().BeTrue();

        store.Get(["node1"])["node1"].Should().ContainSingle().Which.Json.Should().Contain("second");
    }

    [Fact]
    public void Save_WithoutSourceNode_IsIgnored()
    {
        var store = CreateStore(out _);

        store.Save(new NodeMsg { Payload = "hello" }, "", 0).Should().BeFalse();

        store.Get([""]).Should().BeEmpty();
    }

    [Fact]
    public void Get_ReturnsOnlyRequestedNodes()
    {
        var store = CreateStore(out _);

        store.Save(new NodeMsg { Payload = "one" }, "node1", 0);
        store.Save(new NodeMsg { Payload = "two" }, "node2", 0);

        var snapshots = store.Get(["node1"]);

        snapshots.Should().ContainKey("node1");
        snapshots.Should().NotContainKey("node2");
    }

    [Fact]
    public void Get_ExpiredSnapshot_IsDropped()
    {
        var store = CreateStore(out _);

        store.Save(new NodeMsg { Payload = "hello" }, "node1", 0);

        store.Clock = () => DateTime.UtcNow + NodeDebugStore.Ttl + TimeSpan.FromMinutes(1);

        store.Get(["node1"]).Should().BeEmpty();
    }

    [Fact]
    public void Get_AfterExpiryWindow_GoneForEveryNode()
    {
        var store = CreateStore(out _);

        store.Save(new NodeMsg { Payload = "hello" }, "node1", 0);

        store.Clock = () => DateTime.UtcNow + NodeDebugStore.Ttl - TimeSpan.FromMinutes(1);
        store.Get(["node1"]).Should().ContainKey("node1");

        store.Clock = () => DateTime.UtcNow + NodeDebugStore.Ttl + TimeSpan.FromMinutes(1);
        store.Get(["node1"]).Should().BeEmpty();
    }

    private sealed class Mutable { public string Value { get; set; } = ""; }
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

    [Fact]
    public void Enabled_AutoTurnsOffAfterWindow()
    {
        var now = DateTime.UtcNow;
        var mode = new DebugModeState { Clock = () => now };

        mode.Enabled = true;
        mode.Enabled.Should().BeTrue();

        now += DebugModeState.AutoOffAfter - TimeSpan.FromMinutes(1);
        mode.Enabled.Should().BeTrue();

        now += TimeSpan.FromMinutes(2);
        mode.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Enabled_CanBeTurnedOffManually()
    {
        var mode = new DebugModeState { Enabled = true };

        mode.Enabled = false;

        mode.Enabled.Should().BeFalse();
    }
}
