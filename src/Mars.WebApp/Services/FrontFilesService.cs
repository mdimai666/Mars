using Mars.Core.Exceptions;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Shared.Contracts.WebSite.Dto;
using Mars.Shared.Options;
using Mars.WebSiteProcessor.Interfaces;

namespace Mars.Services;

/// <summary>
/// Файловые операции над папкой фронта. Используется REST-контроллером (админка)
/// и ИИ-инструментами. Все пути — только относительные, с проверкой выхода за корень фронта.
/// После изменяющих операций явно уведомляет движок рендера фронта (детерминированная
/// инвалидация кеша и reload предпросмотра — не только через FileSystemWatcher).
/// </summary>
public class FrontFilesService : IFrontFilesService
{
    readonly IFrontManager frontManager;
    readonly IWebRenderEngineLocator renderEngineLocator;

    public FrontFilesService(IFrontManager frontManager, IWebRenderEngineLocator renderEngineLocator)
    {
        this.frontManager = frontManager;
        this.renderEngineLocator = renderEngineLocator;
    }

    public FrontItem GetFront(string slug)
    {
        return frontManager.FindBySlug(slug)
            ?? throw new NotFoundException($"Front '{slug}' not found");
    }

    public string GetFrontRoot(string slug)
    {
        var front = GetFront(slug);
        var root = Path.GetFullPath(frontManager.ResolvePhysicalPath(front));
        if (!Directory.Exists(root))
            throw new NotFoundException($"Front directory '{root}' not found");
        return root;
    }

    /// <summary>
    /// Резолвит относительный путь внутри корня фронта; бросается при выходе за его пределы.
    /// </summary>
    public string ResolveSafePath(string slug, string relPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relPath);

        var root = GetFrontRoot(slug);
        var normalizedRel = relPath.Replace('\\', '/').TrimStart('/');

        var fullPath = Path.GetFullPath(Path.Combine(root, normalizedRel));

        if (fullPath != root && !fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Path '{relPath}' is outside front folder");

        return fullPath;
    }

    public FFrontTreeNodeResponse GetTree(string slug)
    {
        var root = GetFrontRoot(slug);
        return BuildNode(root, root);
    }

    static FFrontTreeNodeResponse BuildNode(string fullPath, string rootPath)
    {
        var isDirectory = Directory.Exists(fullPath);
        var node = new FFrontTreeNodeResponse
        {
            Name = Path.GetFileName(fullPath) ?? "",
            Path = ToRelativePath(fullPath, rootPath),
            IsDirectory = isDirectory,
        };

        if (!isDirectory) return node;

        foreach (var dir in Directory.GetDirectories(fullPath).OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
        {
            if (IsHiddenDirectory(Path.GetFileName(dir))) continue;

            node.Children.Add(BuildNode(dir, rootPath));
        }
        foreach (var file in Directory.GetFiles(fullPath).OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
        {
            node.Children.Add(BuildNode(file, rootPath));
        }

        return node;
    }

    /// <summary>
    /// Стандартные папки (.git, .vscode, node_modules, bin/obj и любые скрытые),
    /// которые не показываются в дереве файлов фронта.
    /// </summary>
    static bool IsHiddenDirectory(string dirName)
        => dirName.StartsWith('.') || dirName is "node_modules" or "bin" or "obj";

    static string ToRelativePath(string fullPath, string rootPath)
    {
        var rel = Path.GetRelativePath(rootPath, fullPath);
        return rel.Replace('\\', '/');
    }

    public FFrontFileContentResponse ReadFile(string slug, string relPath)
    {
        var fullPath = ResolveSafePath(slug, relPath);
        if (!File.Exists(fullPath))
            throw new NotFoundException($"File '{relPath}' not found");

        return new FFrontFileContentResponse
        {
            Path = relPath,
            Content = File.ReadAllText(fullPath),
        };
    }

    public void SaveFile(string slug, string relPath, string content)
    {
        var fullPath = ResolveSafePath(slug, relPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content ?? "");

        NotifyFrontChanged(slug, fullPath);
    }

    public void CreateFile(string slug, string relPath)
    {
        var fullPath = ResolveSafePath(slug, relPath);
        if (File.Exists(fullPath) || Directory.Exists(fullPath))
            throw new ArgumentException($"'{relPath}' already exists");

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "");

        NotifyFrontChanged(slug, fullPath);
    }

    public void CreateFolder(string slug, string relPath)
    {
        var fullPath = ResolveSafePath(slug, relPath);
        if (File.Exists(fullPath) || Directory.Exists(fullPath))
            throw new ArgumentException($"'{relPath}' already exists");

        Directory.CreateDirectory(fullPath);
    }

    public void Rename(string slug, string relPath, string newRelPath)
    {
        var fullPath = ResolveSafePath(slug, relPath);
        var newFullPath = ResolveSafePath(slug, newRelPath);

        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            throw new NotFoundException($"'{relPath}' not found");
        if (File.Exists(newFullPath) || Directory.Exists(newFullPath))
            throw new ArgumentException($"'{newRelPath}' already exists");

        Directory.CreateDirectory(Path.GetDirectoryName(newFullPath)!);

        if (File.Exists(fullPath)) File.Move(fullPath, newFullPath);
        else Directory.Move(fullPath, newFullPath);

        NotifyFrontChanged(slug, newFullPath);
    }

    public void Delete(string slug, string relPath) //TODO: опасно
    {
        var fullPath = ResolveSafePath(slug, relPath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            NotifyFrontChanged(slug, fullPath);
            return;
        }

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
            NotifyFrontChanged(slug, fullPath);
            return;
        }

        throw new NotFoundException($"'{relPath}' not found");
    }

    /// <summary>
    /// Движок рендера (если уже создан) перечитывает шаблон, чистит кеш и шлёт reload
    /// в предпросмотр. Ошибки уведомления не ломают файловую операцию: FileSystemWatcher
    /// остаётся страховкой.
    /// </summary>
    void NotifyFrontChanged(string slug, string fullPath)
    {
        try
        {
            renderEngineLocator.TryGetAppFrontBySlug(slug)
                ?.Features.Get<IWebTemplateService>()
                ?.NotifyFileChanged(fullPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FrontFilesService: notify front '{slug}' file change failed: {ex.Message}");
        }
    }
}
