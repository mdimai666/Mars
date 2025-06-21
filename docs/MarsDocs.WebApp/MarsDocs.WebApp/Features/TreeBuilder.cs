using MarsDocs.WebApp.Models;

namespace MarsDocs.WebApp.Features;

public static class TreeBuilder
{
    public static TreeDirectoryItem[] BuildTree(IEnumerable<string> paths)
    {
        var root = new Dictionary<string, Node>();

        foreach (var path in paths)
        {
            var parts = path.Split('/');
            AddPath(root, parts, 0, "");
        }

        return root.Values.Select(ConvertNodeToItem).ToArray();
    }

    public static IEnumerable<T> BuildTree<T>(IEnumerable<string> paths, Func<TreeDirectoryItem, T> exp)
    {
        var tree = BuildTree(paths);
        foreach (var item in tree)
            yield return exp(item);
    }

    private static void AddPath(Dictionary<string, Node> currentLevel, string[] parts, int index, string currentPath)
    {
        var name = parts[index];
        var fullPath = string.IsNullOrEmpty(currentPath) ? name : $"{currentPath}/{name}";

        if (!currentLevel.TryGetValue(name, out var node))
        {
            node = new Node
            {
                Name = name,
                FullPath = fullPath,
                IsDirectory = index < parts.Length - 1
            };
            currentLevel[name] = node;
        }

        if (index < parts.Length - 1)
        {
            if (node.Children == null)
                node.Children = [];

            AddPath(node.Children, parts, index + 1, fullPath);
        }
    }

    private static TreeDirectoryItem ConvertNodeToItem(Node node)
    {
        return new TreeDirectoryItem(
            node.FullPath,
            node.Name,
            node.IsDirectory,
            node.Children?.Values.Select(ConvertNodeToItem).ToArray()
        );
    }

    private class Node
    {
        public string Name = "";
        public string FullPath = "";
        public bool IsDirectory;
        public Dictionary<string, Node>? Children;
    }

    public static List<TreeDirectoryItem> Flatten(IEnumerable<TreeDirectoryItem> tree)
    {
        var result = new List<TreeDirectoryItem>();

        void Traverse(TreeDirectoryItem item)
        {
            result.Add(item);
            if (item.IsDir && item.Items != null)
            {
                foreach (var child in item.Items)
                {
                    Traverse(child);
                }
            }
        }

        foreach (var item in tree)
        {
            Traverse(item);
        }

        return result;
    }

    public static List<MenuItem> FlattenMenu(IEnumerable<MenuItem> menuItems)
    {
        var result = new List<MenuItem>();

        void Traverse(MenuItem item)
        {
            result.Add(item);
            if (item.SubItems != null)
            {
                foreach (var subItem in item.SubItems)
                {
                    Traverse(subItem);
                }
            }
        }

        foreach (var item in menuItems)
        {
            Traverse(item);
        }

        return result;
    }
}
