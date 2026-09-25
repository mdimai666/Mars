using Mars.Nodes.Front.Abstractions.Services;

namespace Mars.Nodes.FormEditor.EditForms.Components;

public sealed class FieldTreeNode
{
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public string? Type { get; set; }
    public List<FieldTreeNode> Children { get; } = [];
}

public static class ValueFieldTree
{
    public static FieldTreeNode Build(IEnumerable<ValueFieldInfo> fields)
    {
        var root = new FieldTreeNode();

        foreach (var field in fields)
        {
            var parts = field.Path.Split('.');
            var current = root;

            for (var i = 0; i < parts.Length; i++)
            {
                var node = current.Children.FirstOrDefault(c => c.Name == parts[i]);
                if (node is null)
                {
                    node = new FieldTreeNode { Name = parts[i], Path = string.Join('.', parts[..(i + 1)]) };
                    current.Children.Add(node);
                }

                if (i == parts.Length - 1)
                    node.Type = field.VarType;

                current = node;
            }
        }

        return root;
    }

    public static IEnumerable<(FieldTreeNode Node, int Depth, bool Branch)> Rows(FieldTreeNode? root, ISet<string> collapsed)
    {
        if (root is null)
            yield break;

        foreach (var (node, depth) in Walk(root, 0, collapsed))
            yield return (node, depth, node.Children.Count > 0);
    }

    static IEnumerable<(FieldTreeNode Node, int Depth)> Walk(FieldTreeNode node, int depth, ISet<string> collapsed)
    {
        foreach (var child in node.Children)
        {
            yield return (child, depth);

            if (child.Children.Count > 0 && !collapsed.Contains(child.Path))
                foreach (var nested in Walk(child, depth + 1, collapsed))
                    yield return nested;
        }
    }
}
