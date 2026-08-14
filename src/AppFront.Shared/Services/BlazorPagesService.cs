using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;
using AppFront.Shared.Interfaces;
using AppFront.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace AppFront.Shared.Services;

/// <summary>
/// Рефлексия по сборкам: Blazor-страницы (ComponentBase + [Route]), layout-ы, роли,
/// пути к исходникам. Результаты кешируются на сборку.
/// </summary>
public class BlazorPagesService : IBlazorPagesService
{
    /// <summary>Файл в корне репозитория, по которому ищем корень исходников.</summary>
    private const string SolutionFileName = "Mars.slnx";

    private readonly ConcurrentDictionary<Assembly, IReadOnlyList<BlazorPageInfo>> _cache = new();

    private static string? _solutionRoot;
    private static bool _solutionRootChecked;
    private static readonly object SolutionRootLock = new();

    // Однократный индекс файлов src/ (имя файла -> пути) для fallback-поиска исходника
    private static Dictionary<string, List<string>>? _fileIndex;
    private static string? _fileIndexRoot;
    private static readonly object FileIndexLock = new();

    private static readonly Regex GuidInUrlRegex = new(
        @"[{(]?[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public IReadOnlyList<BlazorPageInfo> GetPages(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return _cache.GetOrAdd(assembly, ExtractPages);
    }

    public IReadOnlyList<BlazorPageInfo> GetPages(IEnumerable<Assembly> assemblies)
    {
        return assemblies.SelectMany(GetPages).ToList();
    }

    public IReadOnlyList<BlazorPageInfo> GetRoutedPages(IEnumerable<Assembly> assemblies)
    {
        return GetPages(assemblies)
            .Where(s => s.Kind == EComponentType.Page)
            .ToList();
    }

    public IReadOnlyList<BlazorPageInfo> GetStaticRoutedPages(IEnumerable<Assembly> assemblies)
    {
        return GetRoutedPages(assemblies)
            .Where(s => s.Routes.Any(r => !r.Contains('{')))
            .ToList();
    }

    public IReadOnlyList<BlazorPageInfo> Search(IEnumerable<Assembly> assemblies, string query)
    {
        var pages = GetPages(assemblies);

        if (string.IsNullOrWhiteSpace(query)) return pages;

        return pages.Where(s =>
                s.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || s.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || s.Routes.Any(r => r.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public BlazorPageInfo? FindPageByUrl(IEnumerable<Assembly> assemblies, string url)
    {
        var pages = GetRoutedPages(assemblies);

        string currentUrl = NormalizeUrl(url);
        if (currentUrl.Length == 0) return null;

        var page = pages.FirstOrDefault(s => s.Routes
            .Select(NormalizeRoute)
            .Any(r => r == currentUrl));

        // фолбэк: url короче шаблона (например, guid заменён на '{' — шаблон '/user/{id:guid}' начинается с 'user/{')
        page ??= pages.FirstOrDefault(s => s.Routes
            .Select(NormalizeRoute)
            .Any(r => r.StartsWith(currentUrl, StringComparison.OrdinalIgnoreCase)));

        return page;
    }

    public string? ResolveSourceFilePath(Type pageType)
    {
        ArgumentNullException.ThrowIfNull(pageType);

        var info = GetPages(pageType.Assembly).FirstOrDefault(s => s.PageType == pageType);
        if (info is not null) return info.SourceFilePath ?? info.SourceRelativePath;

        // тип не попал в выборку (например, не ComponentBase) — считаем путь напрямую
        return ResolveAbsoluteSourcePath(pageType) ?? BuildRelativeSourcePath(pageType);
    }

    private IReadOnlyList<BlazorPageInfo> ExtractPages(Assembly assembly)
    {
        var componentBase = typeof(ComponentBase);

        return assembly.GetTypes()
            .Where(p =>
                componentBase.IsAssignableFrom(p)
                && p.IsPublic
                && p.IsClass
                && !p.IsAbstract)
            .Select(type => BuildInfo(type, assembly))
            .ToList();
    }

    private BlazorPageInfo BuildInfo(Type type, Assembly assembly)
    {
        var kind = EComponentType.ComponentBase;
        var routes = new List<string>();

        var attributes = type.GetCustomAttributes().ToList();

        if (type.IsSubclassOf(typeof(LayoutComponentBase)))
        {
            kind = EComponentType.Layout;
        }
        else if (attributes.Any(a => a is RouteAttribute))
        {
            kind = EComponentType.Page;
            routes.AddRange(attributes.OfType<RouteAttribute>().Select(r => r.Template));
        }

        var authorizeAttributes = attributes.OfType<AuthorizeAttribute>().ToList();
        var roles = authorizeAttributes
            .Where(a => a.Roles is not null)
            .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Distinct()
            .ToList();

        var displayAttribute = attributes.OfType<DisplayAttribute>().FirstOrDefault();
        var layoutAttribute = attributes.OfType<LayoutAttribute>().FirstOrDefault();

        return new BlazorPageInfo
        {
            Name = type.Name,
            PageType = type,
            Assembly = assembly,
            Kind = kind,
            Routes = routes,
            Roles = roles,
            RequiresAuthorization = authorizeAttributes.Count > 0,
            AllowsAnonymous = attributes.Any(a => a is AllowAnonymousAttribute),
            LayoutType = layoutAttribute?.LayoutType,
            DisplayName = displayAttribute?.Name ?? SplitCamelCase(Regex.Replace(type.Name, "Page$", "")),
            SourceRelativePath = BuildRelativeSourcePath(type),
            SourceFilePath = ResolveAbsoluteSourcePath(type),
        };
    }

    /// <summary>
    /// Относительный путь к исходнику из namespace: корневое имя сборки остаётся первым
    /// сегментом (папка проекта), остаток namespace превращается в папки.
    /// <c>AppAdmin.Pages.Index</c> → <c>AppAdmin/Pages/Index.razor</c>.
    /// </summary>
    public static string BuildRelativeSourcePath(Type type)
    {
        var assemblyName = type.Assembly.GetName().Name ?? type.Assembly.FullName ?? "";
        var fullName = (type.FullName ?? type.Name).Replace('+', '.');

        var tail = fullName.StartsWith(assemblyName + ".", StringComparison.Ordinal)
            ? fullName[(assemblyName.Length + 1)..]
            : fullName;

        return $"{assemblyName}/{tail.Replace('.', '/')}.razor";
    }

    /// <summary>
    /// Абсолютный путь к исходнику. Best-effort: вне браузера и в Debug-сборке
    /// поднимаемся до корня репозитория (по Mars.slnx) и проверяем кандидата из namespace;
    /// если папки не совпадают с namespace — ищем файл по имени в src/.
    /// </summary>
    private static string? ResolveAbsoluteSourcePath(Type type)
    {
        if (OperatingSystem.IsBrowser() || !Q.IsDevelopment) return null;

        var solutionRoot = TryFindSolutionRoot(type.Assembly);
        if (solutionRoot is null) return null;

        var srcRoot = Path.Combine(solutionRoot, "src");
        if (!Directory.Exists(srcRoot)) return null;

        var candidate = Path.Combine(solutionRoot, "src", BuildRelativeSourcePath(type).Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(candidate)) return candidate;

        // страница может быть чистым C#-классом (.cs вместо .razor)
        var csCandidate = Path.ChangeExtension(candidate, ".cs");
        if (File.Exists(csCandidate)) return csCandidate;

        // папки не совпадают с namespace — ищем по имени файла в заранее построенном индексе
        var fileIndex = GetFileIndex(srcRoot);
        foreach (var extension in new[] { ".razor", ".cs" })
        {
            if (fileIndex.TryGetValue(type.Name + extension, out var matches))
            {
                var found = matches.FirstOrDefault(p => PathMatchesNamespace(p, type));
                if (found is not null) return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Лениво строит (один раз) индекс файлов src/: имя файла -> список полных путей.
    /// Используется, когда namespace не совпадает со структурой папок.
    /// </summary>
    private static Dictionary<string, List<string>> GetFileIndex(string srcRoot)
    {
        lock (FileIndexLock)
        {
            if (_fileIndex is not null && _fileIndexRoot == srcRoot) return _fileIndex;

            var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pattern in new[] { "*.razor", "*.cs" })
            {
                foreach (var file in Directory.EnumerateFiles(srcRoot, pattern, SearchOption.AllDirectories))
                {
                    // артефакты сборки не являются исходниками
                    if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                        continue;

                    var name = Path.GetFileName(file);
                    if (!index.TryGetValue(name, out var list))
                    {
                        list = [];
                        index[name] = list;
                    }
                    list.Add(file);
                }
            }

            _fileIndex = index;
            _fileIndexRoot = srcRoot;
            return index;
        }
    }

    /// <summary>Поднимаемся от каталога сборки до корня репозитория (каталог с Mars.slnx).</summary>
    private static string? TryFindSolutionRoot(Assembly assembly)
    {
        lock (SolutionRootLock)
        {
            if (_solutionRootChecked) return _solutionRoot;
            _solutionRootChecked = true;

            var startDir = !string.IsNullOrEmpty(assembly.Location)
                ? Path.GetDirectoryName(assembly.Location)
                : AppContext.BaseDirectory;

            var dir = startDir is null ? null : new DirectoryInfo(startDir);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
                {
                    _solutionRoot = dir.FullName;
                    break;
                }
                dir = dir.Parent;
            }

            return _solutionRoot;
        }
    }

    /// <summary>Проверяет, что сегменты пути содержат хвост namespace по порядку ( папки ≠ namespace ).</summary>
    private static bool PathMatchesNamespace(string filePath, Type type)
    {
        var ns = type.Namespace;
        if (string.IsNullOrEmpty(ns)) return true;

        var assemblyName = type.Assembly.GetName().Name;
        if (ns == assemblyName) return true;
        if (ns.StartsWith(assemblyName + ".", StringComparison.Ordinal))
            ns = ns[(assemblyName.Length + 1)..];

        var pathParts = filePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var nsParts = ns.Split('.');

        var index = 0;
        foreach (var part in pathParts)
        {
            if (string.Equals(part, nsParts[index], StringComparison.OrdinalIgnoreCase))
            {
                index++;
                if (index == nsParts.Length) return true;
            }
        }

        return false;
    }

    private static string NormalizeUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            url = uri.PathAndQuery;

        url = url.Split('?')[0];
        url = GuidInUrlRegex.Replace(url, "{");

        return url.Trim('/').ToLowerInvariant();
    }

    private static string NormalizeRoute(string route) => route.Trim('/').ToLowerInvariant();

    public static string SplitCamelCase(string input)
    {
        return Regex.Replace(input, "([A-Z]+)", " $1", RegexOptions.Compiled).Trim();
    }
}
