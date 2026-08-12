using Mars.Host.Shared.Dto.Files;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mars.AiChat.Host.Services;

/// <summary>
/// Скилл агента: папка с SKILL.md (YAML-frontmatter name/description + тело-инструкции).
/// </summary>
public sealed record AiSkill(string Name, string Description, string Body);

/// <summary>
/// Каталог скиллов агента: кастомные из <data>/ai/skills + bundled ai-skills рядом со сборкой
/// (кастомный с тем же именем перекрывает bundled). Единый источник для списка в контексте,
/// preload-роутинга по странице и инструментов SearchSkills/LoadSkill
/// (прогрессивное раскрытие, как в Qwen Code CLI).
/// </summary>
public class AiSkillCatalog
{
    private readonly string[] _roots;
    private readonly ILogger<AiSkillCatalog> _logger;

    public AiSkillCatalog(
        [FromKeyedServices("data")] IOptions<FileHostingInfo> dataHostingInfo,
        ILogger<AiSkillCatalog> logger)
    {
        var aiRoot = Path.Combine(dataHostingInfo.Value.PhysicalPath.LocalPath, "ai");
        _roots = [Path.Combine(aiRoot, "skills"), Path.Combine(AppContext.BaseDirectory, "ai-skills")];
        foreach (var root in _roots) Directory.CreateDirectory(root);
        _logger = logger;
    }

    public Task<IReadOnlyList<AiSkill>> GetSkillsAsync(CancellationToken ct)
    {
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
        return Task.FromResult<IReadOnlyList<AiSkill>>(list);
    }

    public async Task<IReadOnlyList<AiSkill>> SearchAsync(string query, CancellationToken ct)
    {
        var all = await GetSkillsAsync(ct);
        if (string.IsNullOrWhiteSpace(query)) return all;

        var tokens = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return all.Where(s =>
        {
            var hay = (s.Name + " " + s.Description).ToLowerInvariant();
            return tokens.All(hay.Contains);
        }).ToList();
    }

    private AiSkill? Parse(string file)
    {
        try
        {
            using var reader = new StringReader(File.ReadAllText(file));
            if (reader.ReadLine()?.Trim() != "---") return null;

            string? name = null, description = null;
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (line.Trim() == "---") break;
                var idx = line.IndexOf(':');
                if (idx <= 0) continue;
                var (key, value) = (line[..idx].Trim(), line[(idx + 1)..].Trim());
                if (key == "name") name = value;
                else if (key == "description") description = value;
            }

            if (name is null || description is null) return null;
            var body = reader.ReadToEnd().TrimStart('\r', '\n').TrimEnd();
            return new AiSkill(name, description, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AiChat: failed to parse skill file {File}", file);
            return null;
        }
    }
}
