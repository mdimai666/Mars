namespace Mars.Nodes.Core.Utils;

public static class NodeWireUtil
{
    /// <summary>
    /// Получает все прямые выходные ноды из текущей.
    /// </summary>
    public static IEnumerable<Node> GetOutputNodes(Node node, IReadOnlyDictionary<string, Node> allNodes)
    {
        if (node?.Wires == null) yield break;

        foreach (var output in node.Wires)
        {
            foreach (var wire in output)
            {
                if (allNodes.TryGetValue(wire.NodeId, out var linkedNode))
                    yield return linkedNode;
            }
        }
    }

    /// <summary>
    /// Получает все связанные ноды (как по входам, так и по выходам).
    /// Рекурсивно обходит граф.
    /// </summary>
    public static HashSet<Node> GetLinkedNodes(Node node, IReadOnlyDictionary<string, Node> allNodes)
    {
        var visited = new HashSet<string>();
        var result = new HashSet<Node>();
        Traverse(node, allNodes, visited, result);
        return result;
    }

    private static void Traverse(Node node, IReadOnlyDictionary<string, Node> allNodes, HashSet<string> visited, HashSet<Node> result)
    {
        if (node == null || !visited.Add(node.Id))
            return;

        result.Add(node);

        // выходящие связи
        foreach (var outNode in GetOutputNodes(node, allNodes))
            Traverse(outNode, allNodes, visited, result);

        // входящие связи
        foreach (var inNode in GetInputNodes(node, allNodes))
            Traverse(inNode, allNodes, visited, result);
    }

    /// <summary>
    /// Получает все прямые входные ноды.
    /// </summary>
    public static IEnumerable<Node> GetInputNodes(Node node, IReadOnlyDictionary<string, Node> allNodes)
    {
        if (node?.Inputs == null) yield break;

        foreach (var potential in allNodes.Values)
        {
            if (potential.Wires == null) continue;

            foreach (var wires in potential.Wires)
            {
                foreach (var wire in wires)
                {
                    if (wire.NodeId == node.Id)
                        yield return potential;
                }
            }
        }
    }

    /// <summary>
    /// Проверяет, есть ли прямое соединение между двумя нодами.
    /// </summary>
    public static bool AreDirectlyConnected(Node a, Node b)
    {
        if (a?.Wires == null) return false;

        return a.Wires.Any(output =>
            output.Any(wire => wire.NodeId == b.Id));
    }

    /// <summary>
    /// Проверяет, есть ли путь между двумя нодами (в любом направлении).
    /// </summary>
    public static bool AreConnected(Node a, Node b, IReadOnlyDictionary<string, Node> allNodes)
    {
        var linked = GetLinkedNodes(a, allNodes);
        return linked.Contains(b);
    }

    /// <summary>
    /// Получает все листья (ноды без выходных связей).
    /// </summary>
    public static IEnumerable<Node> GetLeafNodes(IReadOnlyDictionary<string, Node> allNodes)
    {
        return allNodes.Values.Where(n => n.Wires == null || n.Wires.All(w => w.Count == 0));
    }

    /// <summary>
    /// Получает все корневые ноды (у которых нет входящих связей).
    /// </summary>
    public static IEnumerable<Node> GetRootNodes(IReadOnlyDictionary<string, Node> allNodes)
    {
        return allNodes.Values.Where(n => !GetInputNodes(n, allNodes).Any());
    }

    public static Wire[] DrawWires(IReadOnlyDictionary<string, Node> nodes,
                                    INodeWirePointResolver nodeWirePointResolver)
    {
        if (nodeWirePointResolver == null)
            throw new ArgumentNullException(nameof(nodeWirePointResolver));

        var wires = new List<Wire>();

        foreach (var node in nodes.Values)
        {
            if (node.Wires == null) continue;

            for (int outputIndex = 0; outputIndex < node.Wires.Count; outputIndex++)
            {
                var outputWires = node.Wires[outputIndex];
                if (outputWires == null) continue;

                foreach (var wire in outputWires)
                {
                    if (!nodes.TryGetValue(wire.NodeId, out var targetNode))
                        continue;

                    // Используем резолвер для расчета точек
                    var points = nodeWirePointResolver.GetPoints(node, outputIndex, targetNode, wire.PortIndex);

                    var newWire = new Wire
                    {
                        Id = $"{node.Id}->{wire}", // уникальный ID провода
                        Node1 = new NodeWire(node.Id, outputIndex),
                        Node2 = wire,

                        X1 = points.Start.X,
                        Y1 = points.Start.Y,
                        X2 = points.End.X,
                        Y2 = points.End.Y
                    };

                    wires.Add(newWire);
                }
            }
        }

        return wires.ToArray();
    }
}
