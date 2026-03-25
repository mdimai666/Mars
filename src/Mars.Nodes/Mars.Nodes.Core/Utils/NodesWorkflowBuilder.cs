using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Utils;

public class NodesWorkflowBuilder
{
    public IEnumerable<Node> Nodes => _nodes.Values.Select(s => s.Node);
    Dictionary<string, BuilderNodeItem> _nodes { get; set; } = [];
    public int LastCreatedGeneration { get; private set; } = -1;
    public IReadOnlyDictionary<string, BuilderNodeItem> BuilderItems => _nodes;

    private NodesWorkflowBuilder() { }

    public static NodesWorkflowBuilder Create()
    {
        return new NodesWorkflowBuilder();
    }

    /// <summary>
    /// add wired with previous generation nodes
    /// </summary>
    /// <param name="nodes"></param>
    /// <returns></returns>
    public NodesWorkflowBuilder AddNext(params Node[] nodes)
        => AddNext(nodes, false);

    /// <summary>
    /// add without wiring
    /// </summary>
    /// <param name="nodes"></param>
    /// <returns></returns>
    public NodesWorkflowBuilder Add(params Node[] nodes)
        => AddNext(nodes, true);

    private NodesWorkflowBuilder AddNext(Node[] nodes, bool allowUnlink)
    {
        if (_nodes.Count == 0)
        {
            LastCreatedGeneration = 0;
            var _index = 0;
            foreach (var item in nodes)
            {
                _nodes.Add(item.Id, new(item, LastCreatedGeneration, _index));
                _index++;
            }
            return this;
        }

        var nextNodes = GetLastGenerationOutputables();
        if (!allowUnlink && nextNodes.Count() == 0) throw new InvalidOperationException("not have any nodes with output");

        var index = _nodes.Values.Count(s => s.Generation == LastCreatedGeneration + 1);
        foreach (var newNode in nodes)
        {
            _nodes.Add(newNode.Id, new(newNode, LastCreatedGeneration + 1, index));
            foreach (var node in nextNodes)
            {
                if (node.Outputs.Any())
                    node.Wires.First().Add(new(newNode.Id));
            }
            index++;
        }
        LastCreatedGeneration++;

        return this;
    }

    public NodesWorkflowBuilder AddNext() => AddNext(new TemplateNode());

    public NodesWorkflowBuilder AddNext(params NodesWorkflowBuilder[] builders)
    {
        if (_nodes.Count == 0)
        {
            LastCreatedGeneration = 0;
        }

        var nextNodes = GetLastGenerationOutputables();

        var line = 0;
        foreach (var builder in builders)
        {
            int startElementIndex = line;

            foreach (var item in builder.BuilderItems.Values)
            {
                var gen = item.Generation + LastCreatedGeneration + 1;
                var elementRowIndex = item.ElementRowIndex + startElementIndex;
                _nodes.Add(item.Node.Id, new BuilderNodeItem(item.Node, gen, elementRowIndex));

                if (item.Generation == 0)
                {
                    foreach (var node in nextNodes)
                    {
                        if (node.Outputs.Any())
                            node.Wires.First().Add(new(item.Node.Id));
                    }
                }
            }
            line += Math.Max(_nodes.Values.Max(s => s.ElementRowIndex), 1);
        }

        LastCreatedGeneration = _nodes.Values.Max(s => s.Generation);

        return this;
    }

    public NodesWorkflowBuilder AddIndependent(Node firstNode, params Node[] otherNodes)
    {
        var index = 0;
        foreach (var node in (IEnumerable<Node>)[firstNode, .. otherNodes])
        {
            _nodes.Add(node.Id, new(node, LastCreatedGeneration, index));
            index++;
        }

        return this;
    }

    public IEnumerable<Node> LastGeneration()
        => _nodes.Values.Where(s => s.Generation == LastCreatedGeneration)
                        .Select(s => s.Node);

    public Node[] Build()
    {
        var wireWidth = 110;
        var heightOffset = 60;
        var prevGenerationMaxWidth = 0f;

        for (var generation = 0; generation <= LastCreatedGeneration; generation++)
        {
            var nodes = _nodes.Values.Where(s => s.Generation == generation).ToArray();

            foreach (var nodeBuilder in _nodes.Values)
            {
                nodeBuilder.Node.X = (prevGenerationMaxWidth + wireWidth) * nodeBuilder.Generation;
                nodeBuilder.Node.Y = (heightOffset) * nodeBuilder.ElementRowIndex;
            }

            var generationMaxWidth = nodes.Max(s => CalcBodyWidth(s.Node));
            prevGenerationMaxWidth = generationMaxWidth;
        }

        return Nodes.ToArray();
    }

    public Node[] BuildWithFlowNode()
    {
        var flowNode = new FlowNode();
        var nodes = Build().ToList();
        nodes.ToList().ForEach(s => s.Container = flowNode.Id);

        return [flowNode, .. nodes];
    }

    internal Node[] GetLastGenerationOutputables()
        => _nodes.Values.Where(s => s.Generation == LastCreatedGeneration)
                        .Where(s => s.Node.Outputs.Any())
                        .Select(s => s.Node)
                        .ToArray();

    public record BuilderNodeItem
    {
        public Node Node { get; init; }
        public int Generation { get; set; }
        public int ElementRowIndex { get; set; }

        public BuilderNodeItem(Node node, int generation, int elementRowIndex)
        {
            Node = node;
            Generation = generation;
            ElementRowIndex = elementRowIndex;
        }
    };

    public static float CalcBodyWidth(Node node) => Math.Min(360, Math.Max(120, node.DisplayName.Length * 9 + 40));
}
