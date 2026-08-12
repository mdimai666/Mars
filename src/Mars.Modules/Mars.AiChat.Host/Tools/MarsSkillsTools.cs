using System.ComponentModel;
using Mars.AiChat.Host.Services;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструменты каталога скиллов: поиск по имени/описанию и загрузка полных инструкций.
/// </summary>
public class MarsSkillsTools
{
    private readonly AiSkillCatalog _catalog;

    public MarsSkillsTools(AiSkillCatalog catalog) => _catalog = catalog;

    [Description("Поиск по каталогу скиллов агента: возвращает имена и описания подходящих скиллов. " +
                 "Пустой запрос — весь каталог. Используй, когда не уверен, какой скилл подходит под задачу.")]
    public async Task<string> SearchSkills(
        [Description("Поисковый запрос (слова из имени/описания), пусто — весь каталог")] string query = "",
        CancellationToken ct = default)
    {
        var found = await _catalog.SearchAsync(query, ct);
        if (found.Count == 0) return "Скиллы не найдены. Пустой запрос вернёт весь каталог.";

        return string.Join("\n", found.Select(s => $"- {s.Name}: {s.Description}"));
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
