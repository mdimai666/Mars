using System.ComponentModel;
using Mars.AiChat.Host.Services;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструменты каталога скиллов: поиск по имени/описанию и загрузка полных инструкций.
/// </summary>
public class MarsSkillsTools
{
    private const int MaxSearchResults = 20;

    private readonly AiSkillCatalog _catalog;

    public MarsSkillsTools(AiSkillCatalog catalog) => _catalog = catalog;

    [Description("Поиск по каталогу скиллов агента: возвращает имена и описания подходящих скиллов (до 20). " +
                 "Пустой запрос — первые 20 каталога. Используй, когда не уверен, какой скилл подходит под задачу.")]
    public async Task<string> SearchSkills(
        [Description("Поисковый запрос (слова из имени/описания/тегов), пусто — начало каталога")] string query = "",
        CancellationToken ct = default)
    {
        var found = await _catalog.SearchAsync(query, ct);
        if (found.Count == 0) return "Скиллы не найдены. Попробуй другие слова или пустой запрос.";

        var sb = new System.Text.StringBuilder();
        foreach (var s in found.Take(MaxSearchResults))
        {
            sb.Append("- ").Append(s.Name).Append(": ").Append(s.Description);
            if (s.Tags.Count > 0) sb.Append(" (теги: ").Append(string.Join(", ", s.Tags)).Append(')');
            sb.AppendLine();
        }

        if (found.Count > MaxSearchResults)
            sb.Append($"…и ещё {found.Count - MaxSearchResults} — уточни запрос.");

        return sb.ToString().TrimEnd();
    }

    [Description("Загрузить полные инструкции скилла по имени (из каталога скиллов). " +
                 "После загрузки следуй инструкциям скилла.")]
    public async Task<string> LoadSkill(
        [Description("Имя скилла, например mars-posts")] string skillName,
        CancellationToken ct = default)
    {
        var all = await _catalog.GetSkillsAsync(ct);
        var skill = all.FirstOrDefault(s => string.Equals(s.Name, skillName, StringComparison.OrdinalIgnoreCase));
        if (skill is null)
            return $"Скилл '{skillName}' не найден. Доступны: {string.Join(", ", all.Select(s => s.Name))}";

        return skill.Body;
    }
}
