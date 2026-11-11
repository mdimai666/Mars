#if !NOADMIN
using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Mars.Admin.Framework.Interfaces;
using Mars.Admin.Framework.Models;
using Mars.CommandLine.Abstractions;
using Mars.Core.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Admin.Host.CommandLine;

/// <summary>
/// `Mars.exe pages` — Blazor-страницы/компоненты для агентов-разработчиков:
/// маршруты, роли, layout, пути к исходникам и код. Данные из IBlazorPagesService
/// (тот же источник, что у DevTestPage), сборки — админка + фронт-сборки плагинов,
/// загруженные в серверный процесс.
/// </summary>
public class PagesCommandCli : CommandCli
{
    public const string CommandName = "pages";

    /// <summary>Префикс маунта админки (MapWhen "/dev" в MainAdmin).</summary>
    internal const string MountPrefix = "/dev";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public PagesCommandCli(ICommandLineApi cli) : base(cli)
    {
        var optionJson = new Option<bool>("--json") { Description = "machine-readable JSON output" };
        var optionFilter = new Option<string>("--filter", "-f") { Description = "regex filter on name/displayName/routes" };
        var optionAssembly = new Option<string>("--assembly", "-a") { Description = "assembly name substring filter" };
        var optionKind = new Option<string>("--kind", "-k")
        {
            Description = "page (default) | layout | component | all",
            DefaultValueFactory = _ => "page",
        };
        var optionStatic = new Option<bool>("--static") { Description = "only pages with a parameterless route (crawlable)" };
        var optionCode = new Option<bool>("--code") { Description = "print source file contents (.razor + .razor.cs)" };
        var optionUrl = new Option<string>("--url")
        {
            Required = true,
            Description = "page URL (mount prefix optional; GUIDs are matched to route templates)",
        };
        var argumentQuery = new Argument<string>("query") { Description = "page class name, display name or route" };

        var pagesCommand = new Command(CommandName, "Blazor pages/components: routes, roles, layout, source paths and code (dev helper)");

        var listCommand = new Command("list", "list pages (default), layouts or components")
        {
            optionFilter, optionAssembly, optionKind, optionStatic, optionJson,
        };
        listCommand.SetAction((p, ct) => ListCommand(
            p.GetValue(optionFilter), p.GetValue(optionAssembly), p.GetValue(optionKind),
            p.GetValue(optionStatic), p.GetValue(optionJson)));
        pagesCommand.Subcommands.Add(listCommand);

        var showCommand = new Command("show", "page card: routes, auth, layout, source path; --code prints the source")
        {
            argumentQuery, optionCode, optionJson,
        };
        showCommand.SetAction((p, ct) => ShowCommand(p.GetRequiredValue(argumentQuery), p.GetValue(optionCode), p.GetValue(optionJson)));
        pagesCommand.Subcommands.Add(showCommand);

        var findCommand = new Command("find", "find the page serving a URL") { optionUrl, optionCode, optionJson };
        findCommand.SetAction((p, ct) => FindCommand(p.GetRequiredValue(optionUrl), p.GetValue(optionCode), p.GetValue(optionJson)));
        pagesCommand.Subcommands.Add(findCommand);

        var layoutsCommand = new Command("layouts", "list layouts and how many pages use them") { optionJson };
        layoutsCommand.SetAction((p, ct) => LayoutsCommand(p.GetValue(optionJson)));
        pagesCommand.Subcommands.Add(layoutsCommand);

        cli.AddCommand(pagesCommand);
    }

    private Task ListCommand(string? filter, string? assembly, string? kind, bool staticOnly, bool json)
    {
        var pages = GetAllPages();

        IReadOnlyList<BlazorPageInfo> selected;
        try
        {
            selected = FilterPages(pages, kind, assembly, staticOnly, filter);
        }
        catch (ArgumentException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(ex.Message);
            Console.ResetColor();
            return Task.CompletedTask;
        }

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(selected.Select(p => BuildDto(p, LayoutSource(p))), JsonOptions));
            return Task.CompletedTask;
        }

        if (selected.Count == 0)
        {
            Console.WriteLine("No pages matched.");
            return Task.CompletedTask;
        }

        var rows = selected.Select(p => new[]
        {
            p.Name,
            string.Join(", ", p.Routes),
            AuthText(p),
            p.LayoutType?.Name ?? "",
            p.SourceFilePath ?? p.SourceRelativePath ?? "",
        }).ToList();

        Console.WriteLine(new ConsoleTable([["Name", "Routes", "Auth", "Layout", "Source"], .. rows]));
        return Task.CompletedTask;
    }

    private Task ShowCommand(string query, bool code, bool json)
    {
        var pages = GetAllPages();
        var page = ResolveSingle(pages, query, out var candidates);

        if (page is null)
        {
            if (candidates.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"No page found for '{query}'.");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"{candidates.Count} matches for '{query}' — specify the class name or route exactly:");
                var rows = candidates.Select(p => new[]
                {
                    p.Name,
                    string.Join(", ", p.Routes),
                    p.SourceFilePath ?? p.SourceRelativePath ?? "",
                }).ToList();
                Console.WriteLine(new ConsoleTable([["Name", "Routes", "Source"], .. rows]));
            }
            return Task.CompletedTask;
        }

        PrintPage(page, code, json);
        return Task.CompletedTask;
    }

    private Task FindCommand(string url, bool code, bool json)
    {
        var service = app.Services.GetRequiredService<IBlazorPagesService>();
        var page = service.FindPageByUrl(GetBlazorAssemblies(), url);

        if (page is null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"No page found for URL '{url}'.");
            Console.ResetColor();
            return Task.CompletedTask;
        }

        PrintPage(page, code, json);
        return Task.CompletedTask;
    }

    private Task LayoutsCommand(bool json)
    {
        var pages = GetAllPages();
        var layouts = pages.Where(p => p.Kind == EComponentType.Layout).ToList();

        if (json)
        {
            var dto = layouts.Select(l => new
            {
                name = l.Name,
                assembly = l.Assembly.GetName().Name,
                usedByPages = pages.Count(p => p.LayoutType == l.PageType),
                source = l.SourceFilePath ?? l.SourceRelativePath,
            });
            Console.WriteLine(JsonSerializer.Serialize(dto, JsonOptions));
            return Task.CompletedTask;
        }

        var rows = layouts.Select(l => new[]
        {
            l.Name,
            l.Assembly.GetName().Name ?? "",
            pages.Count(p => p.LayoutType == l.PageType).ToString(),
            l.SourceFilePath ?? l.SourceRelativePath ?? "",
        }).ToList();

        Console.WriteLine(new ConsoleTable([["Name", "Assembly", "UsedByPages", "Source"], .. rows]));
        return Task.CompletedTask;
    }

    private void PrintPage(BlazorPageInfo page, bool code, bool json)
    {
        var layoutSource = LayoutSource(page);

        if (json)
        {
            var dto = BuildDto(page, layoutSource);
            if (code)
            {
                var files = GetSourceFilePaths(page)
                    .Select(path => new { path, content = File.ReadAllText(path) })
                    .ToList();
                Console.WriteLine(JsonSerializer.Serialize(new { page = dto, code = files }, JsonOptions));
            }
            else
            {
                Console.WriteLine(JsonSerializer.Serialize(dto, JsonOptions));
            }
            return;
        }

        Console.WriteLine($"{page.Name}  ({page.DisplayName})");
        Console.WriteLine($"  Assembly : {page.Assembly.GetName().Name}");
        Console.WriteLine($"  Kind     : {page.Kind}");
        Console.WriteLine($"  Routes   : {string.Join(", ", page.Routes.Select(r => $"{r}  ->  {MountPrefix}{r}"))}");
        Console.WriteLine($"  Auth     : {AuthText(page)}");
        Console.WriteLine($"  Layout   : {(page.LayoutType is null ? "(default)" : $"{page.LayoutType.Name}  ({layoutSource ?? "?"})")}");
        Console.WriteLine($"  Source   : {page.SourceFilePath ?? page.SourceRelativePath ?? "(unknown)"}");

        if (!code) return;

        var sourceFiles = GetSourceFilePaths(page);
        if (sourceFiles.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Source files not found on disk (Release build? relative path: " +
                $"{page.SourceRelativePath ?? "?"})");
            Console.ResetColor();
            return;
        }

        foreach (var path in sourceFiles)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {path} ---");
            Console.WriteLine(File.ReadAllText(path));
        }
    }

    private IReadOnlyList<BlazorPageInfo> GetAllPages()
    {
        var service = app.Services.GetRequiredService<IBlazorPagesService>();
        var result = new List<BlazorPageInfo>();
        foreach (var assembly in GetBlazorAssemblies())
        {
            try
            {
                result.AddRange(service.GetPages(assembly));
            }
            catch (ReflectionTypeLoadException)
            {
                // сборка с неполностью загруженными зависимостями — пропускаем, не валим команду
            }
        }
        return result;
    }

    private string? LayoutSource(BlazorPageInfo page)
    {
        if (page.LayoutType is null) return null;
        var service = app.Services.GetRequiredService<IBlazorPagesService>();
        return service.ResolveSourceFilePath(page.LayoutType);
    }

    private static string AuthText(BlazorPageInfo page)
    {
        if (page.Roles.Count > 0) return "roles: " + string.Join(", ", page.Roles);
        if (page.RequiresAuthorization) return "login";
        return "anonymous";
    }

    /// <summary>
    /// Админка + Blazor-сборки, загруженные в процесс (фронт-сборки плагинов грузит
    /// PluginManager). Системные и Microsoft-сборки отсекаем — это шум, а не страницы приложения.
    /// </summary>
    internal static IEnumerable<Assembly> GetBlazorAssemblies()
    {
        var adminAssembly = typeof(Mars.Admin.App).Assembly;
        yield return adminAssembly;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic || assembly == adminAssembly) continue;

            var name = assembly.GetName().Name ?? "";
            if (name.StartsWith("Microsoft.") || name.StartsWith("System.")
                || name.StartsWith("netstandard") || name.StartsWith("mscorlib")) continue;

            bool referencesComponents;
            try
            {
                referencesComponents = assembly.GetReferencedAssemblies()
                    .Any(a => a.Name == "Microsoft.AspNetCore.Components");
            }
            catch
            {
                continue;
            }

            if (referencesComponents) yield return assembly;
        }
    }

    internal static IReadOnlyList<BlazorPageInfo> FilterPages(
        IReadOnlyList<BlazorPageInfo> pages, string? kind, string? assembly, bool staticOnly, string? filter)
    {
        IEnumerable<BlazorPageInfo> result = kind?.ToLowerInvariant() switch
        {
            null or "" or "page" => pages.Where(p => p.Kind == EComponentType.Page),
            "layout" => pages.Where(p => p.Kind == EComponentType.Layout),
            "component" => pages.Where(p => p.Kind == EComponentType.ComponentBase),
            "all" => pages,
            _ => throw new ArgumentException($"Unknown kind '{kind}'. Use: page | layout | component | all"),
        };

        if (!string.IsNullOrWhiteSpace(assembly))
        {
            result = result.Where(p => (p.Assembly.GetName().Name ?? "")
                .Contains(assembly, StringComparison.OrdinalIgnoreCase));
        }

        if (staticOnly)
        {
            result = result.Where(p => p.Routes.Any(r => !r.Contains('{')));
        }

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var regex = new Regex(filter, RegexOptions.IgnoreCase);
            result = result.Where(p =>
                regex.IsMatch(p.Name)
                || regex.IsMatch(p.DisplayName)
                || p.Routes.Any(r => regex.IsMatch(r)));
        }

        return result.ToList();
    }

    /// <summary>
    /// Точное имя класса → точный маршрут → поиск по подстроке. Ровно одно совпадение —
    /// страница; иначе null и список кандидатов для уточнения.
    /// </summary>
    internal static BlazorPageInfo? ResolveSingle(
        IReadOnlyList<BlazorPageInfo> pages, string query, out IReadOnlyList<BlazorPageInfo> candidates)
    {
        candidates = pages.Where(p => p.Name.Equals(query, StringComparison.OrdinalIgnoreCase)).ToList();
        if (candidates.Count == 1) return candidates[0];

        if (candidates.Count == 0)
        {
            var route = query.Trim('/');
            candidates = pages.Where(p => p.Routes.Any(r => r.Trim('/').Equals(route, StringComparison.OrdinalIgnoreCase))).ToList();
            if (candidates.Count == 1) return candidates[0];
        }

        if (candidates.Count == 0)
        {
            candidates = pages.Where(p =>
                    p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || p.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || p.Routes.Any(r => r.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (candidates.Count == 1) return candidates[0];
        }

        return null;
    }

    /// <summary>
    /// Существующие файлы исходника: основной (.razor или .cs) + code-behind (.razor.cs).
    /// Абсолютный путь из сервиса, иначе fallback от cwd: src/&lt;SourceRelativePath&gt;.
    /// </summary>
    internal static List<string> GetSourceFilePaths(BlazorPageInfo page)
    {
        var path = page.SourceFilePath;
        if (path is null && page.SourceRelativePath is not null)
        {
            var candidate = Path.Combine("src", page.SourceRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate)) path = Path.GetFullPath(candidate);
        }

        var files = new List<string>();
        if (path is null || !File.Exists(path)) return files;

        files.Add(path);
        if (path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
        {
            var codeBehind = path + ".cs";
            if (File.Exists(codeBehind)) files.Add(codeBehind);
        }
        return files;
    }

    internal static object BuildDto(BlazorPageInfo page, string? layoutSource) => new
    {
        name = page.Name,
        displayName = page.DisplayName,
        assembly = page.Assembly.GetName().Name,
        kind = page.Kind.ToString(),
        routes = page.Routes,
        urls = page.Routes.Select(r => MountPrefix + r).ToList(),
        roles = page.Roles,
        requiresAuthorization = page.RequiresAuthorization,
        allowsAnonymous = page.AllowsAnonymous,
        layout = page.LayoutType is null ? null : new { name = page.LayoutType.Name, source = layoutSource },
        source = page.SourceFilePath ?? page.SourceRelativePath,
    };
}
#endif
