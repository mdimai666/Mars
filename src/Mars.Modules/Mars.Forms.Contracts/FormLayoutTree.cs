namespace Mars.Forms.Contracts;

/// <summary>Узел раскладки вместе с детьми: представление плоского списка деревом</summary>
public record FormLayoutNode
{
    public required FormItem Item { get; init; }

    public IReadOnlyList<FormLayoutNode> Children { get; init; } = [];
}

/// <summary>
/// Проекция плоской раскладки в дерево — одна на всех, кто рисует: рендерер формы и дизайнер.
/// Порядок узлов — порядок списка; дети — узлы с <see cref="FormItem.Parent"/>, равным ключу узла.
/// Каждый узел зоны попадает в дерево ровно один раз: недостижимый по родителям (осиротевший
/// после ручной правки json) становится корневым, поэтому проекция ничего не теряет.
/// </summary>
public static class FormLayoutTree
{
    /// <summary>Дерево зоны: её корневые узлы (без родителя) и их дети</summary>
    public static IReadOnlyList<FormLayoutNode> Build(IEnumerable<FormItem> items, string? zone)
    {
        var all = items.ToList();

        var children = new Dictionary<string, List<FormItem>>(StringComparer.Ordinal);
        foreach (var item in all)
        {
            if (item.Parent is not { } parent) continue;
            if (!children.TryGetValue(parent, out var list)) children[parent] = list = [];
            list.Add(item);
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var roots = new List<FormLayoutNode>();

        foreach (var item in all)
        {
            if (item.Parent is not null || !InZone(item)) continue;
            if (Node(item) is { } node) roots.Add(node);
        }

        // узлы, до которых не дошло (нет родителя в списке или цикл), показываем корневыми
        foreach (var item in all)
        {
            if (visited.Contains(item.Key) || !InZone(item)) continue;
            if (Node(item) is { } node) roots.Add(node);
        }

        return roots;

        bool InZone(FormItem item) => (item.Zone ?? "") == (zone ?? "");

        FormLayoutNode? Node(FormItem item)
        {
            if (!visited.Add(item.Key)) return null;

            return new FormLayoutNode
            {
                Item = item,
                Children = children.TryGetValue(item.Key, out var kids)
                    ? kids.Select(Node).OfType<FormLayoutNode>().ToList()
                    : [],
            };
        }
    }
}
