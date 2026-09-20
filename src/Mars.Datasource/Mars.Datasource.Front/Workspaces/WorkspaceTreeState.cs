using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Front.Workspaces;

/// <summary>
/// Состояние дерева объектов: фильтр, свёрнутые группы и вычисления для разметки.
/// Группа по умолчанию из профиля источника (у sql это схема: public, у MsSQL — dbo) раскрыта,
/// остальные сворачиваются один раз на источник: дальше состояние принадлежит пользователю,
/// иначе свёрнутая вручную группа разворачивалась бы после каждого обновления. Если такой группы
/// в каталоге нет (в MySQL схема — это сама база, у файла группа одна и без имени),
/// дерево остаётся раскрытым. Провайдер может задать порог <see cref="DatasourceKindProfile.CollapseGroupsAbove"/>:
/// на каталоге не больше порога дерево раскрыто целиком (rest сворачивает методы, только когда
/// операций больше 25, — раскрытым остаётся GET).
/// </summary>
public class WorkspaceTreeState
{
    public string Filter { get; set; } = "";

    public string? DefaultsFor { get; private set; }

    readonly HashSet<string> _collapsed = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Раскрыта ли группа. При активном фильтре дерево раскрыто целиком: иначе найденный
    /// объект остался бы спрятанным в свёрнутой группе.</summary>
    public bool IsExpanded(string group)
        => !string.IsNullOrWhiteSpace(Filter) || !_collapsed.Contains(group);

    public void Toggle(string group)
    {
        if (!_collapsed.Remove(group)) _collapsed.Add(group);
    }

    /// <summary>Открытый объект должен быть виден, даже если его группу свернули.</summary>
    public void Expand(string group) => _collapsed.Remove(group);

    public IEnumerable<DatasourceCatalogGroup> Groups(DatasourceCatalog catalog)
    {
        foreach (var group in catalog.Groups.OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase))
        {
            var objects = Filtered(group);

            if (objects.Count == 0) continue;

            yield return new DatasourceCatalogGroup { Name = group.Name, Objects = objects };
        }
    }

    public static bool HasMultipleGroups(DatasourceCatalog catalog)
        => catalog.Groups.Count(group => group.Objects.Count > 0) > 1;

    List<DatasourceCatalogObject> Filtered(DatasourceCatalogGroup group)
        => string.IsNullOrWhiteSpace(Filter)
            ? group.Objects
            : group.Objects.Where(o => o.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase)).ToList();

    public void ApplyDefaults(string? key, DatasourceCatalog catalog)
    {
        if (DefaultsFor == key) return;

        DefaultsFor = key;
        _collapsed.Clear();

        // Группу по умолчанию называет провайдер (у sql это схема движка), у остальных источников её нет.
        var defaultSchema = catalog.Profile.DefaultGroup;

        if (string.IsNullOrEmpty(defaultSchema)) return;

        var groups = catalog.Groups
            .Select(group => group.Name)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();

        if (!groups.Contains(defaultSchema, StringComparer.OrdinalIgnoreCase)) return;

        // Провайдер может свернуть дерево только на большом каталоге (rest: больше 25 операций —
        // раскрытым остаётся GET), маленький каталог виден целиком.
        if (catalog.Profile.CollapseGroupsAbove is > 0 and var above
            && catalog.Groups.Sum(group => group.Objects.Count) <= above) return;

        foreach (var group in groups)
        {
            if (!string.Equals(group, defaultSchema, StringComparison.OrdinalIgnoreCase))
            {
                _collapsed.Add(group);
            }
        }
    }
}
