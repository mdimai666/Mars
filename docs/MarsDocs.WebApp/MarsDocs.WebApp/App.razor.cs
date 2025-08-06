using System.Reflection;
using MarsDocs.WebApp.Features;
using MarsDocs.WebApp.Models;
using Microsoft.JSInterop;

namespace MarsDocs.WebApp;

public partial class App
{
    public static readonly string MarsVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0";

    public static MenuItem[] Menu { get; private set; } = default!;
    public static Dictionary<string, MenuItem> MenuDict { get; private set; } = default!;

    public static event Action<string>? OnReload;

    public App()
    {
        var mdFiles = GetAllMdFilesInFolder();
        //var index = ReadEmbedFile("Startup.md");
        Console.WriteLine($"Found {mdFiles.Where(f => f.EndsWith(".md", StringComparison.OrdinalIgnoreCase)).Count()} markdown files.");

        var tree = TreeBuilder.BuildTree(mdFiles, ToMenu).ToArray();

        //tree.Where(s => s.FileName == "QuickStart.md").ToList().ForEach(s => s.SetOrder(1f));

        Menu = tree;
        MenuDict = TreeBuilder.FlattenMenu(Menu).ToDictionary(s => s.Path);
    }

    public List<string> GetAllMdFilesInFolder()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames();

        var namespaceLength = typeof(App).Namespace!.Length + 1;

        return resources.Where(r => r.EndsWith(".md"))
                        .Select(s => s.Substring(namespaceLength))
                        .ToList();
    }

    public static string ReadEmbedFile(string resourcePath)
    {
        var _namespace = typeof(App).Namespace!;
        var embedPath = _namespace + '.' + resourcePath;

        var assembly = Assembly.GetExecutingAssembly();
        using Stream stream = assembly.GetManifestResourceStream(embedPath)!
                                    ?? throw new FileNotFoundException($"resource not found '{resourcePath}'({embedPath})");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    static MenuItem ToMenu(TreeDirectoryItem item)
    {
        var meta = !item.IsDir ? MarkdownMetadataReader.ReadMetadata(ReadEmbedFile(item.FullPath)) : null;

        return new(
            item.FullPath,
            $"md/{item.FullPath}",
            item.Name,
            meta?.GetValueOrDefault("Title") ?? Path.GetFileNameWithoutExtension(item.Name),
            IsDivider: false,
            SubItemFlag: item.IsDir,
            item.Items?.Select(ToMenu).ToArray() ?? Array.Empty<MenuItem>(),
            float.TryParse(meta?.GetValueOrDefault("Order"), out var order) ? order : 10f
        );
    }

    [JSInvokable("NotifyReload")]
    public static void NotifyReload(string value)
    {
        Console.WriteLine($"[SseBridge] Reload received: {value}");
        OnReload?.Invoke(value);
    }
}
