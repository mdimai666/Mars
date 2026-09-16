using Mars.Nodes.Core;
using Mars.Nodes.Front.Abstractions.Services;

namespace Mars.Nodes.Workspace.Services.ValueFields;

/// <summary>
/// Walks wires up from the edited node and collects what nodes declare they put into the message.
/// Nearest declaration wins per path; <see cref="NodeOutputValueSpecReader.Fallback"/> only when
/// nobody declared the Payload slot.
/// </summary>
internal class MsgValueRootProvider(IHostValueHints hostHints) : IValueRootProvider
{
    public const string RootName = "msg";

    public int Order => 0;

    public IEnumerable<ValueFieldInfo> GetFields(ValueFieldContext context)
    {
        var (specs, payloadDeclared) = Walk(context);

        foreach (var (spec, source) in specs)
            yield return new ValueFieldInfo($"{RootName}.{spec.Path}", spec.VarType, source);

        if (payloadDeclared) yield break;

        foreach (var spec in NodeOutputValueSpecReader.Fallback)
            yield return new ValueFieldInfo($"{RootName}.{spec.Path}", spec.VarType);
    }

    (List<(OutputValueSpec Spec, string? Source)> Specs, bool PayloadDeclared) Walk(ValueFieldContext context)
    {
        var specs = new List<(OutputValueSpec, string?)>();
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

            foreach (var spec in SpecsOf(node))
            {
                if (spec.OutputPort != OutputValueSpec.AllOutputPorts && spec.OutputPort != port) continue;
                if (!paths.Add(spec.Path)) continue;

                if (spec.Path == nameof(NodeMsg.Payload)) payloadDeclared = true;
                specs.Add((spec, node.DisplayName));
            }

            foreach (var source in Sources(context.Nodes, node.Id))
                queue.Enqueue(source);
        }

        return (specs, payloadDeclared);
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
