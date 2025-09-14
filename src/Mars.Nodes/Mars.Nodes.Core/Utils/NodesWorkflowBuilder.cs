using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Utils;

public class NodesWorkflowBuilder
{
    public IEnumerable<Node> Nodes => _nodes.Values.Select(s => s.Node);
    Dictionary<string, BuilderNodeItem> _nodes { get; set; } = [];
    public int LastCreatedGeneration { get; private set; } = -1;

    private NodesWorkflowBuilder() { }

    public static NodesWorkflowBuilder Create()
    {
        return new NodesWorkflowBuilder();
    }

    public NodesWorkflowBuilder AddNext(params Node[] nodes)
    {
        if (_nodes.Count == 0)
        {
            LastCreatedGeneration = 0;
            foreach (var item in nodes)
            {
                _nodes.Add(item.Id, new(item, LastCreatedGeneration));
            }
            return this;
        }

        var nextNodes = GetLastGenerationOutputables();
        if (nextNodes.Count() == 0) throw new InvalidOperationException("not have any nodes with output");

        foreach (var newNode in nodes)
        {
            _nodes.Add(newNode.Id, new(newNode, LastCreatedGeneration + 1));
            foreach (var node in nextNodes)
            {
                node.Wires.First().Add(new(newNode.Id));
            }
        }
        LastCreatedGeneration++;

        return this;
    }

    public NodesWorkflowBuilder AddNext() => AddNext(new TemplateNode());

    public NodesWorkflowBuilder AddIndependent(Node firstNode, params Node[] otherNodes)
    {
        foreach (var node in (IEnumerable<Node>)[firstNode, .. otherNodes])
            _nodes.Add(node.Id, new(node, LastCreatedGeneration));

        return this;
    }

    public IEnumerable<Node> LastGeneration()
        => _nodes.Values.Where(s => s.Generation == LastCreatedGeneration)
                        .Select(s => s.Node);

    public Node[] Build() => Nodes.ToArray();

    internal Node[] GetLastGenerationOutputables()
        => _nodes.Values.Where(s => s.Generation == LastCreatedGeneration)
                        .Where(s => s.Node.Outputs.Any())
                        .Select(s => s.Node)
                        .ToArray();

    private record BuilderNodeItem(Node Node, int Generation);
}
