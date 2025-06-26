using System.Reflection;
using MarsDocs.WebApp.Features;
using MarsDocs.WebApp.Models;

namespace MarsDocs.WebApp;

public partial class App
{
    public static MenuItem[] Menu { get; private set; } = default!;
    public static Dictionary<string, MenuItem> MenuDict { get; private set; } = default!;

    public App()
    {
        var mdFiles = GetAllMdFilesInFolder();
        //var index = ReadEmbedFile("Startup.md");
        Console.WriteLine($"Found {mdFiles.Where(f => f.EndsWith(".md", StringComparison.OrdinalIgnoreCase)).Count()} markdown files.");

        var tree = TreeBuilder.BuildTree(mdFiles, ToMenu).ToArray();

        Menu = tree;
        MenuDict = TreeBuilder.FlattenMenu(Menu).ToDictionary(s => s.Path);
    }

    public List<string> GetAllMdFilesInFolder()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames();

        var namespaceLength = typeof(App).Namespace!.Length + 1;

        return resources.Where(r => r.EndsWith(".md"))
                        .Select(s => s.Substring(namespaceLength)
                                        .Replace(Path.DirectorySeparatorChar, '/'))
                        .ToList();
    }

    public static string ReadEmbedFile(string resourcePath)
    {
        var _namespace = typeof(App).Namespace!;
        var embedPath = _namespace + '.' + resourcePath.Replace('/', Path.DirectorySeparatorChar);

        var assembly = Assembly.GetExecutingAssembly();
        using Stream stream = assembly.GetManifestResourceStream(embedPath)!
                                    ?? throw new FileNotFoundException($"resource not found '{resourcePath}'({embedPath})");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    static MenuItem ToMenu(TreeDirectoryItem item)
        => new(
            item.FullPath,
            $"md/{item.FullPath}",
            item.Name,
            Path.GetFileNameWithoutExtension(item.Name),
            IsDivider: false,
            SubItemFlag: item.IsDir,
            item.Items?.Select(ToMenu).ToArray() ?? Array.Empty<MenuItem>()
        );
}
