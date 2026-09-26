using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Front.Abstractions.Editor.Models;

namespace Mars.Nodes.Front.Abstractions.Editor;

/// <summary>
/// Документ нод редактора: первичный словарь нод и производные коллекции
/// (потоки, ноды активного потока, var-ноды, граф link-нод).
/// Мутации не уведомляют никого сами: редактор после вызова решает,
/// что пересчитать (<see cref="Recalculate"/>) и кого оповестить.
/// Платформенное (URL-навигация, ActionManager, workspace) сюда не входит.
/// </summary>
public class NodesDocument
{
    Dictionary<string, Node> _nodes = [];

    /// <summary>Живой словарь нод — тот же экземпляр отдаётся наружу (INodeEditorApi.AllNodes).</summary>
    public IDictionary<string, Node> Nodes => _nodes;

    public IReadOnlyList<FlowNode> Flows { get; private set; } = [];
    public FlowNode? ActiveFlow { get; private set; }
    public IReadOnlyDictionary<string, Node> FlowNodes { get; private set; } = new Dictionary<string, Node>();
    public IReadOnlyCollection<VarNode> VarNodes { get; private set; } = [];
    public IReadOnlyDictionary<string, LinkInNode[]> InboundLinkOutNodes { get; private set; } = new Dictionary<string, LinkInNode[]>();

    /// <summary>Подменяет словарь целиком. Возвращает false, если это тот же экземпляр (ничего не сделано).</summary>
    public bool SetNodes(IDictionary<string, Node> nodes)
    {
        if (ReferenceEquals(nodes, _nodes)) return false;
        _nodes = nodes as Dictionary<string, Node> ?? nodes.ToDictionary(s => s.Key, s => s.Value);
        return true;
    }

    public void AddNode(Node node) => _nodes.Add(node.Id, node);

    public void AddNodesAndWires(IEnumerable<Node> nodes, IEnumerable<NodeConnect> connects)
    {
        foreach (var node in nodes)
            _nodes.Add(node.Id, node);

        foreach (var w in connects)
            _nodes[w.Node1.NodeId].Wires[w.Node1.PortIndex].Add(w.Node2);
    }

    public void DeleteNodesAndWires(IEnumerable<Node> nodes, IEnumerable<NodeConnect> connects)
    {
        var nodeIds = nodes.Select(s => s.Id).ToHashSet();

        // сносим висячие ссылки у выживших нод
        foreach (var node in _nodes.Values)
        {
            if (nodeIds.Contains(node.Id)) continue;
            foreach (var wireOuts in node.Wires)
                wireOuts.RemoveAll(s => nodeIds.Contains(s.NodeId));
        }

        foreach (var node in nodes)
            _nodes.Remove(node.Id);

        foreach (var w in connects)
        {
            if (_nodes.TryGetValue(w.Node1.NodeId, out var source))
                source.Wires[w.Node1.PortIndex].RemoveAll(s => s == w.Node2);
        }
    }

    /// <summary>Заменяет ноду в словаре и пересчитывает только затронутые производные.</summary>
    public void SaveNode(Node node)
    {
        _nodes[node.Id] = node;

        if (node is FlowNode)
        {
            CalcFlows();
        }
        else
        {
            CalcVarNodes();
            CalcFlowNodes();
            if (node is LinkInNode or LinkOutNode) CalcLinkNodesGraph();
        }
    }

    public void Recalculate()
    {
        CalcFlows();
        CalcFlowNodes();
        CalcVarNodes();
        CalcLinkNodesGraph();
    }

    /// <summary>Переключение активного потока (навигацию по URL делает редактор).</summary>
    public void ChangeFlow(FlowNode flowNode)
    {
        ActiveFlow = flowNode;
        CalcFlowNodes();
        CalcVarNodes();
    }

    /// <summary>
    /// Резолвит активный поток, если его ещё нет: сначала preferredFlowId (из query-строки URL),
    /// иначе первый по Order. Возвращает false, если поток уже был выбран.
    /// </summary>
    public bool TryResolveActiveFlow(string? preferredFlowId)
    {
        if (ActiveFlow is not null) return false;

        ActiveFlow = (!string.IsNullOrEmpty(preferredFlowId) ? Flows.FirstOrDefault(s => s.Id == preferredFlowId) : null)
                     ?? Flows.FirstOrDefault();
        CalcFlowNodes();
        return true;
    }

    public IReadOnlyDictionary<string, Node> GetFlowNodes(string? flowId)
        => flowId == null
            ? new Dictionary<string, Node>()
            : _nodes.Values.Where(s => s.IsVisual
                                    && s.Container == flowId
                                    && (s is not UnknownNode || (s is UnknownNode un && !un.IsDefinedAsConfig)))
                                .ToDictionary(s => s.Id);

    void CalcFlows() => Flows = _nodes.Values.OfType<FlowNode>().OrderBy(s => s.Order).ToList();

    void CalcFlowNodes() => FlowNodes = GetFlowNodes(ActiveFlow?.Id);

    void CalcVarNodes() => VarNodes = _nodes.Values.OfType<VarNode>().OrderBy(s => s.Name).ToList();

    void CalcLinkNodesGraph()
    {
        var inboundLinkOutNodesDict = new Dictionary<string, List<LinkInNode>>();
        foreach (var linkInNode in _nodes.Values.OfType<LinkInNode>())
        {
            foreach (var outId in linkInNode.OutLinksIds)
            {
                if (!inboundLinkOutNodesDict.TryGetValue(outId, out var list))
                    inboundLinkOutNodesDict[outId] = list = [];
                list.Add(linkInNode);
            }
        }
        InboundLinkOutNodes = inboundLinkOutNodesDict.ToDictionary(s => s.Key, s => s.Value.ToArray());
    }
}
