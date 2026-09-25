namespace Mars.Nodes.Core.Utils;

/// <summary>
/// Множество id нод для снятия debug-снапшотов: стартовые ноды плюс всё замыкание
/// вверх по входящим проводам (кто транзитивно подаёт на них сообщения).
/// </summary>
public static class DebugSnapshotScopeUtil
{
    public static HashSet<string> BuildUpstreamScope(IEnumerable<Node> nodes, params string?[]? startIds)
    {
        var upstream = new Dictionary<string, List<string>>();
        foreach (var node in nodes)
        {
            foreach (var port in node.Wires)
            {
                foreach (var wire in port)
                {
                    if (!upstream.TryGetValue(wire.NodeId, out var sources))
                        upstream[wire.NodeId] = sources = [];
                    sources.Add(node.Id);
                }
            }
        }

        var scope = new HashSet<string>();
        foreach (var startId in startIds ?? [])
            AddUpstreamClosure(scope, startId, upstream);
        return scope;
    }

    static void AddUpstreamClosure(HashSet<string> scope, string? startId, IReadOnlyDictionary<string, List<string>> upstream)
    {
        if (startId is null) return;

        var queue = new Queue<string>();
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!scope.Add(id)) continue;

            if (upstream.TryGetValue(id, out var sources))
            {
                foreach (var source in sources) queue.Enqueue(source);
            }
        }
    }
}
