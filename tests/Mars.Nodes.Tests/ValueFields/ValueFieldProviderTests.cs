using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Contracts.Nodes;
using Mars.Nodes.Front.Abstractions.Services;
using Mars.Nodes.Workspace.Services.ValueFields;

namespace Mars.Nodes.Tests.ValueFields;

public class ValueFieldProviderTests
{
    static readonly HostValueHints EmptyHostHints = new();

    static ValueFieldProvider MsgProvider(IHostValueHints? hints = null)
        => new([new MsgValueRootProvider(hints ?? EmptyHostHints)]);

    static NodeDebugSnapshotsResponse Snapshots(params NodeDebugSnapshot[] snapshots)
        => new()
        {
            ServerTimeUtc = DateTime.UtcNow,
            Snapshots = snapshots.GroupBy(s => s.NodeId).ToDictionary(g => g.Key, g => g.ToArray()),
        };

    static ValueFieldContext Context(Node edited, params Node[] nodes)
    {
        var dict = nodes.ToDictionary(node => node.Id);
        dict[edited.Id] = edited;
        return new ValueFieldContext(dict, edited);
    }

    static void Wire(Node from, Node to, int fromPort = 0)
    {
        while (from.Wires.Count <= fromPort) from.Wires.Add([]);

        from.Wires[fromPort].Add(new NodeWire(to.Id));
    }

    [Fact]
    public void GetFields_CollectsFieldsDeclaredUpstream()
    {
        var inject = new InjectNode
        {
            Fields =
            [
                new() { Key = "Payload", Value = "hello" },
                new() { Key = "count", VarType = "int", Value = "1" },
            ]
        };
        var eval = new EvalNode();
        Wire(inject, eval);

        var fields = MsgProvider().GetFields(Context(eval, inject));

        fields.Select(f => (f.Path, f.VarType)).Should().Equal(
            ("msg.Payload", "string"),
            ("msg.count", "int"));
    }

    [Fact]
    public void GetFields_TransitNodePassesUpstreamFieldsThrough()
    {
        var inject = new InjectNode { Fields = [new() { Key = "Payload", Value = "hello" }] };
        var switchNode = new SwitchNode();
        var eval = new EvalNode();
        Wire(inject, switchNode);
        Wire(switchNode, eval);

        var fields = MsgProvider().GetFields(Context(eval, inject, switchNode));

        fields.Select(f => (f.Path, f.VarType)).Should().Equal(("msg.Payload", "string"));
    }

    [Fact]
    public void GetFields_NobodyDeclaresPayload_FallsBackToObjectPayload()
    {
        var inject = new InjectNode { Fields = [new() { Key = "status", Value = "ok" }] };
        var eval = new EvalNode();
        Wire(inject, eval);

        var fields = MsgProvider().GetFields(Context(eval, inject));

        fields.Select(f => (f.Path, f.VarType)).Should().Equal(
            ("msg.status", "string"),
            ("msg.Payload", "object"));
    }

    [Fact]
    public void GetFields_NearestDeclarationWins()
    {
        var far = new InjectNode { Fields = [new() { Key = "Payload", VarType = "string", Value = "a" }] };
        var near = new InjectNode { Fields = [new() { Key = "Payload", VarType = "int", Value = "1" }] };
        var eval = new EvalNode();
        Wire(far, near);
        Wire(near, eval);

        var fields = MsgProvider().GetFields(Context(eval, far, near));

        fields.Select(f => (f.Path, f.VarType)).Should().Equal(("msg.Payload", "int"));
    }

    [Fact]
    public void GetFields_PayloadDeclaredOnAnotherPort_FallsBackToUnknownPayload()
    {
        var node = new TwoOutputNode();
        var eval = new EvalNode();
        Wire(node, eval, fromPort: 1);

        var fields = MsgProvider().GetFields(Context(eval, node));

        fields.Select(f => (f.Path, f.VarType)).Should().BeEquivalentTo(new[]
        {
            ("msg.count", "int"),
            ("msg.flag", "bool"),
            ("msg.Payload", "object"),
        });
    }

    [Fact]
    public void GetFields_DeclaredPayload_IsNotReplacedByFallback()
    {
        var node = new TwoOutputNode();
        var eval = new EvalNode();
        Wire(node, eval, fromPort: 0);

        var fields = MsgProvider().GetFields(Context(eval, node));

        fields.Select(f => (f.Path, f.VarType)).Should().BeEquivalentTo(new[]
        {
            ("msg.Payload", "string"),
            ("msg.flag", "bool"),
        });
    }

    [Fact]
    public void GetFields_HostSpecs_UsedWhenLocalTypeDeclaresNothing()
    {
        var hints = new HostValueHints();
        hints.SetOutputSpecs(new Dictionary<string, OutputValueSpec[]>
        {
            ["core.SwitchNode"] = [new OutputValueSpec("Payload", "string")],
        });

        var switchNode = new SwitchNode();
        var eval = new EvalNode();
        Wire(switchNode, eval);

        var fields = MsgProvider(hints).GetFields(Context(eval, switchNode));

        fields.Select(f => (f.Path, f.VarType)).Should().Equal(("msg.Payload", "string"));
    }

    [Fact]
    public void GetFields_CircularWires_DoNotLoopForever()
    {
        var inject = new InjectNode { Fields = [new() { Key = "Payload", Value = "hello" }] };
        var eval = new EvalNode();
        Wire(inject, eval);
        Wire(eval, inject);

        var fields = MsgProvider().GetFields(Context(eval, inject));

        fields.Select(f => f.Path).Should().Equal("msg.Payload");
    }

    [Fact]
    public void GetFields_Source_NamesTheDeclaringNode()
    {
        var inject = new InjectNode { Fields = [new() { Key = "Payload", Value = "hello" }] };
        var eval = new EvalNode();
        Wire(inject, eval);

        var fields = MsgProvider().GetFields(Context(eval, inject));

        fields.Should().OnlyContain(f => f.Source == inject.DisplayName);
    }

    [Fact]
    public void GetFields_FlowContext_NamesComeFromVariableSetNode()
    {
        var setter = new VariableSetNode { Setters = [new() { ValuePath = "FlowContext.z" }] };
        var eval = new EvalNode();
        var provider = new ValueFieldProvider([new FlowContextValueRootProvider()]);

        var fields = provider.GetFields(Context(eval, setter));

        fields.Select(f => f.Path).Should().Equal("FlowContext.z");
    }

    [Fact]
    public void GetFields_GlobalContext_NamesComeFromGraphAndHost()
    {
        var setter = new VariableSetNode { Setters = [new() { ValuePath = "GlobalContext.q" }] };
        var eval = new EvalNode();
        var hints = new HostValueHints();
        hints.SetGlobalVariableNames(["fromHost"]);
        var provider = new ValueFieldProvider([new GlobalContextValueRootProvider(hints)]);

        var fields = provider.GetFields(Context(eval, setter));

        fields.Select(f => (f.Path, f.VarType)).Should().Equal(
            ("GlobalContext.q", "object"),
            ("GlobalContext.fromHost", "object"));
    }

    [Fact]
    public void GetFields_VarNode_TakesTypeFromVarType()
    {
        var varNode = new VarNode { Name = "siteName", VarType = "string" };
        var eval = new EvalNode();
        var provider = new ValueFieldProvider([new VarNodeValueRootProvider()]);

        var fields = provider.GetFields(Context(eval, varNode));

        fields.Should().Equal(new ValueFieldInfo("VarNode.siteName", "string", varNode.DisplayName));
    }

    [Fact]
    public void GetFields_Composite_OrdersByRootAndDropsDuplicates()
    {
        var inject = new InjectNode { Fields = [new() { Key = "Payload", Value = "hello" }] };
        var varNode = new VarNode { Name = "siteName", VarType = "string" };
        var eval = new EvalNode();
        Wire(inject, eval);

        var provider = new ValueFieldProvider(
        [
            new VarNodeValueRootProvider(),
            new DuplicateVarNodeValueRootProvider(),
            new MsgValueRootProvider(EmptyHostHints),
        ]);

        var fields = provider.GetFields(Context(eval, inject, varNode));

        fields.Should().Equal(
            new ValueFieldInfo("msg.Payload", "string", inject.DisplayName),
            new ValueFieldInfo("VarNode.siteName", "string", varNode.DisplayName));
    }

    [Fact]
    public void GetFields_WithDebugSnapshot_AttachesValuesToDeclaredPaths()
    {
        var inject = new InjectNode
        {
            Fields =
            [
                new() { Key = "Payload", Value = "hello" },
                new() { Key = "count", VarType = "int", Value = "1" },
            ]
        };
        var eval = new EvalNode();
        Wire(inject, eval);

        var hints = new HostValueHints();
        var msg = new NodeMsg { Payload = "hello" };
        msg.Set("count", 42);
        hints.SetDebugSnapshots(Snapshots(NodeDebugSnapshotBuilder.Build(msg, inject.Id, 0)));

        var fields = MsgProvider(hints).GetFields(Context(eval, inject));

        fields.Should().Equal(
            new ValueFieldInfo("msg.Payload", "string", inject.DisplayName, "hello"),
            new ValueFieldInfo("msg.count", "int", inject.DisplayName, "42"));
    }

    [Fact]
    public void GetFields_SnapshotOnlyPaths_AreAddedWithValues()
    {
        var inject = new InjectNode { Fields = [new() { Key = "Payload", Value = "hello" }] };
        var eval = new EvalNode();
        Wire(inject, eval);

        var hints = new HostValueHints();
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
        hints.SetDebugSnapshots(Snapshots(NodeDebugSnapshotBuilder.Build(msg, inject.Id, 0)));

        var fields = MsgProvider(hints).GetFields(Context(eval, inject));

        fields.Should().Contain(new ValueFieldInfo("msg.Payload.user.email", "object", inject.DisplayName, "a@b.c"));
        fields.Should().Contain(new ValueFieldInfo("msg.Payload.items[1].name", "object", inject.DisplayName, "two"));
        fields.Should().Contain(new ValueFieldInfo("msg.Payload.items", "object", inject.DisplayName, "[2 items]"));
    }

    [Fact]
    public void GetFields_SnapshotOfAnotherPort_IsNotUsed()
    {
        var inject = new InjectNode { Fields = [new() { Key = "Payload", Value = "hello" }] };
        var eval = new EvalNode();
        Wire(inject, eval, fromPort: 0);

        var hints = new HostValueHints();
        hints.SetDebugSnapshots(Snapshots(
            NodeDebugSnapshotBuilder.Build(new NodeMsg { Payload = "other" }, inject.Id, 1)));

        var fields = MsgProvider(hints).GetFields(Context(eval, inject));

        fields.Should().Equal(new ValueFieldInfo("msg.Payload", "string", inject.DisplayName));
    }

    private sealed class DuplicateVarNodeValueRootProvider : IValueRootProvider
    {
        public int Order => 40;

        public IEnumerable<ValueFieldInfo> GetFields(ValueFieldContext context)
            => [new ValueFieldInfo("VarNode.siteName", "string", "duplicate")];
    }

    [NodeOutputValueSpec(typeof(string))]
    [NodeOutputValueSpec(typeof(int), Name = "count", OutputPort = 1)]
    [NodeOutputValueSpec(typeof(bool), Name = "flag", OutputPort = OutputValueSpec.AllOutputPorts)]
    private sealed class TwoOutputNode : Node
    {
        public TwoOutputNode()
        {
            Outputs = [new(), new()];
        }
    }
}
