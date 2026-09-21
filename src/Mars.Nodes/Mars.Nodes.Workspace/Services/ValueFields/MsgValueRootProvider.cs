using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Front.Abstractions.Services;

namespace Mars.Nodes.Workspace.Services.ValueFields;

/// <summary>
/// Walks wires up from the edited node and collects what nodes declare they put into the message.
/// Nearest declaration wins per path; <see cref="NodeOutputValueSpecReader.Fallback"/> only when
/// nobody declared the Payload slot. If a debug snapshot exists for a node and port, its values are
/// attached to the matching paths and the paths that only exist in the live data are added too.
/// </summary>
internal class MsgValueRootProvider(IHostValueHints hostHints) : IValueRootProvider
{
    public const string RootName = "msg";

    public int Order => 0;

    public IEnumerable<ValueFieldInfo> GetFields(ValueFieldContext context)
    {
        var (fields, payloadDeclared) = Walk(context);

        foreach (var (spec, source, value) in fields)
            yield return new ValueFieldInfo($"{RootName}.{spec.Path}", spec.VarType, source, value);

        if (payloadDeclared) yield break;

        foreach (var spec in NodeOutputValueSpecReader.Fallback)
            yield return new ValueFieldInfo($"{RootName}.{spec.Path}", spec.VarType);
    }

    (List<(OutputValueSpec Spec, string? Source, string? Value)> Fields, bool PayloadDeclared) Walk(
        ValueFieldContext context)
    {
        var fields = new List<(OutputValueSpec, string?, string?)>();
        var paths = new HashSet<string>();
        var visited = new HashSet<string> { context.EditedNode.Id };
        var queue = new Queue<(Node Node, int Port)>();

        foreach (var source in Sources(context.Nodes, context.EditedNode.Id))
            queue.Enqueue(source);

        var payloadDeclared = false;

        while (queue.Count > 0)
        {
            var (node, port) = queue.Dequeue();
            if (!visited.Add(node.Id)) continue;

            var values = Values(node, port);

            foreach (var spec in SpecsOf(node))
            {
                if (spec.OutputPort != OutputValueSpec.AllOutputPorts && spec.OutputPort != port) continue;
                if (!paths.Add(spec.Path)) continue;

                if (spec.Path == nameof(NodeMsg.Payload)) payloadDeclared = true;
                fields.Add((spec, node.DisplayName, values?.GetValueOrDefault(spec.Path)));
            }

            if (values is not null)
            {
                foreach (var (path, value) in values)
                {
                    if (!paths.Add(path)) continue;

                    if (path == nameof(NodeMsg.Payload)) payloadDeclared = true;
                    fields.Add((new OutputValueSpec(path, VarNode.ObjectTypeName), node.DisplayName, value));
                }
            }

            foreach (var source in Sources(context.Nodes, node.Id))
                queue.Enqueue(source);
        }

        return (fields, payloadDeclared);
    }

    Dictionary<string, string>? Values(Node node, int port)
    {
        var snapshot = hostHints.GetDebugSnapshot(node.Id, port);

        return snapshot is null ? null : DebugSnapshotValues.Flatten(snapshot.Json);
    }

    IEnumerable<OutputValueSpec> SpecsOf(Node node)
    {
        var specs = NodeOutputValueSpecReader.Read(node);
        return specs.Count > 0 ? specs : hostHints.GetOutputSpecs(node.TypeId);
    }

    static IEnumerable<(Node Node, int Port)> Sources(IDictionary<string, Node> nodes, string nodeId)
    {
        foreach (var node in nodes.Values)
        {
            for (var port = 0; port < node.Wires.Count; port++)
            {
                foreach (var wire in node.Wires[port])
                {
                    if (wire.NodeId == nodeId) yield return (node, port);
                }
            }
        }
    }
}
