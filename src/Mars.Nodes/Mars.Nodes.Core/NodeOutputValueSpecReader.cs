using System.Collections.Concurrent;
using System.Reflection;

namespace Mars.Nodes.Core;

/// <summary>
/// Reads what a node declares it puts into the message: the instance interface, then the class attribute.
/// Empty result means the node adds nothing (transit node), not an error.
/// </summary>
public static class NodeOutputValueSpecReader
{
    static readonly ConcurrentDictionary<Type, IReadOnlyList<OutputValueSpec>> StaticsCache = new();

    /// <summary>Used when no node in the chain declares the Payload slot.</summary>
    public static IReadOnlyList<OutputValueSpec> Fallback { get; } =
        [new OutputValueSpec("Payload", VarNode.ObjectTypeName)];

    public static IReadOnlyList<OutputValueSpec> Read(Node node)
        => node is INodeOutputValueSpec nodeSpec
            ? nodeSpec.GetOutputValueSpec().DistinctBy(s => (s.Path, s.OutputPort)).ToArray()
            : ReadStatics(node.GetType());

    public static IReadOnlyList<OutputValueSpec> ReadStatics(Type nodeOrImplementType)
        => StaticsCache.GetOrAdd(nodeOrImplementType, static type =>
        {
            var specs = new List<OutputValueSpec>();

            foreach (var attribute in type.GetCustomAttributes<NodeOutputValueSpecAttribute>(true))
            {
                foreach (var spec in OutputValueSpecExpander.Expand(attribute.Name, attribute.ValueType,
                                                                    outputPort: attribute.OutputPort))
                    specs.Add(attribute.Description is null ? spec : spec with { Description = attribute.Description });
            }

            return specs.DistinctBy(s => (s.Path, s.OutputPort)).ToArray();
        });
}
