using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes.Common;

public class ConfigNodeImpl : INodeImplement<ConfigNode>
{

    public ConfigNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public ConfigNodeImpl(ConfigNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        throw new NotSupportedException("ConfigNode not executable");
    }
}
