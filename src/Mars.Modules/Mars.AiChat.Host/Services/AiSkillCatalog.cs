using Mars.Contracts.Dto.Files;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mars.AiChat.Host.Services;

/// <summary>
/// Скилл агента: папка с SKILL.md (YAML-frontmatter name/description/tags + тело-инструкции).
/// </summary>
public sealed record AiSkill(string Name, string Description, IReadOnlyList<string> Tags, string Body);

/// <summary>
/// Каталог скиллов агента: кастомные из <data>/ai/skills + bundled ai-skills рядом со сборкой
/// (кастомный с тем же именем перекрывает bundled). Единый источник для списка в контексте,
/// preload-роутинга по странице и инструментов SearchSkills/LoadSkill
/// (прогрессивное раскрытие, как в Qwen Code CLI).
/// Сканирует папки один раз и держит индекс в памяти; FileSystemWatcher сбрасывает кеш
/// при изменении файлов (правка скилла подхватывается без перезапуска).
/// </summary>
public class AiSkillCatalog : IDisposable
{
    private readonly string[] _roots;
    private readonly ILogger<AiSkillCatalog> _logger;
    private readonly FileSystemWatcher[] _watchers;
    private readonly object _lock = new();
    private List<AiSkill>? _cache;

    public AiSkillCatalog(
        [FromKeyedServices("data")] IOptions<FileHostingInfo> dataHostingInfo,
        ILogger<AiSkillCatalog> logger)
    {
        var aiRoot = Path.Combine(dataHostingInfo.Value.PhysicalPath.LocalPath, "ai");
        _roots = [Path.Combine(aiRoot, "skills"), Path.Combine(AppContext.BaseDirectory, "ai-skills")];
        foreach (var root in _roots) Directory.CreateDirectory(root);
        _logger = logger;

        _watchers = _roots.Select(root =>
        {
            var watcher = new FileSystemWatcher(root)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            };
            watcher.Created += OnChanged;
            watcher.Changed += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnChanged;
            watcher.EnableRaisingEvents = true;
            return watcher;
        }).ToArray();
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        lock (_lock) _cache = null;
    }

    public Task<IReadOnlyList<AiSkill>> GetSkillsAsync(CancellationToken ct)
    {
        lock (_lock)
        {
            if (_cache is not null) return Task.FromResult<IReadOnlyList<AiSkill>>(_cache);

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<AiSkill>();

            foreach (var root in _roots)
            {
                foreach (var dir in Directory.GetDirectories(root))
                {
                    var file = Path.Combine(dir, "SKILL.md");
                    if (!File.Exists(file)) continue;

                    var skill = Parse(file);
                    if (skill is null) continue;
                    if (!seen.Add(skill.Name)) continue; // кастомные идут первыми и перекрывают bundled
                    list.Add(skill);
                }
            }

            list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            _cache = list;
            return Task.FromResult<IReadOnlyList<AiSkill>>(list);
        }
    }

    public async Task<IReadOnlyList<AiSkill>> SearchAsync(string query, CancellationToken ct)
    {
        var all = await GetSkillsAsync(ct);
        if (string.IsNullOrWhiteSpace(query)) return all;

        var tokens = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return all.Where(s =>
        {
            var hay = (s.Name + " " + s.Description + " " + string.Join(" ", s.Tags)).ToLowerInvariant();
            return tokens.All(hay.Contains);
        }).ToList();
    }

    /// <summary>
    /// Список описаний для системного промпта: featured (в заданном порядке) первыми,
    /// остальные по имени, суммарно не больше <paramref name="max"/> (0 — не показывать).
    /// На большом каталоге хвост остаётся за SearchSkills — контекст не раздувается.
    /// </summary>
    public static string BuildContextListing(IReadOnlyList<AiSkill> all, IReadOnlyList<string> featured, int max)
    {
        if (max <= 0 || all.Count == 0) return "";

        var featuredSet = new HashSet<string>(featured, StringComparer.OrdinalIgnoreCase);
        var ordered = all.Where(s => featuredSet.Contains(s.Name))
            .OrderBy(s => Array.IndexOf([.. featured], s.Name))
            .Concat(all.Where(s => !featuredSet.Contains(s.Name)))
            .ToList();

        var sb = new System.Text.StringBuilder();
        foreach (var s in ordered.Take(max))
            sb.AppendLine($"- {s.Name}: {s.Description}");

        if (ordered.Count > max)
            sb.Append($"…и ещё {ordered.Count - max} — ищи через SearchSkills.");

        return sb.ToString().TrimEnd();
    }

    private AiSkill? Parse(string file)
    {
        try
        {
            using var reader = new StringReader(File.ReadAllText(file));
            if (reader.ReadLine()?.Trim() != "---") return null;

            string? name = null, description = null;
            var tags = new List<string>();
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (line.Trim() == "---") break;
                var idx = line.IndexOf(':');
                if (idx <= 0) continue;
                var (key, value) = (line[..idx].Trim(), line[(idx + 1)..].Trim());
                if (key == "name") name = value;
                else if (key == "description") description = value;
                else if (key == "tags")
                    tags.AddRange(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            if (name is null || description is null) return null;
            var body = reader.ReadToEnd().TrimStart('\r', '\n').TrimEnd();
            return new AiSkill(name, description, tags, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AiChat: failed to parse skill file {File}", file);
            return null;
        }
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers) watcher.Dispose();
    }
}
