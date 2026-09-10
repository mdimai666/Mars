using Mars.Contracts.Resources;
using Mars.Forms.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Дизайнер раскладки формы: дерево, которое отдаёт провайдер (<see cref="FormDefinition"/>),
/// правится по зонам — порядок, зона, видимость, ширина, секции и переопределение заголовка.
/// Параметры самих полей (правила, редактор) здесь не редактируются — они живут в настройках
/// владельца формы (<see cref="FormFieldSettings"/>) и правятся на странице типа.
/// Наружу уходит только раскладка (<see cref="FormLayoutSettings"/>): дескрипторы не хранятся,
/// их каждый раз отдаёт провайдер. Доступные зоны — из манифеста провайдера.
/// </summary>
public partial class FormLayoutEditor
{
    [Inject] IStringLocalizer<AppRes> L { get; set; } = default!;

    /// <summary>Нормализованное дерево провайдера: уже с применённой сохранённой раскладкой</summary>
    [Parameter] public FormDefinition? Definition { get; set; }

    /// <summary>Раскладка после каждого изменения; null — сброс к раскладке провайдера</summary>
    [Parameter] public EventCallback<FormLayoutSettings?> ValueChanged { get; set; }

    /// <summary>
    /// Просьба сбросить раскладку: хост подменяет <see cref="Definition"/> на дерево по умолчанию
    /// (дизайнер не знает порядка провайдера — дерево собирает сервер).
    /// </summary>
    [Parameter] public EventCallback OnResetRequested { get; set; }

    readonly List<LayoutRow> _rows = [];
    List<FormZoneDescriptor> _zones = [];
    FormDefinition? _source;

    /// <summary>Зона, в которую добавляется новая секция</summary>
    public string NewSectionZone { get; set; } = "";

    public IReadOnlyList<FormZoneDescriptor> Zones => _zones;

    /// <summary>Варианты ширины элемента в зоне (пустой ключ — на всю ширину)</summary>
    public static readonly IReadOnlyList<(string Key, string Title)> Widths =
    [
        ("", "—"),
        (FormItemWidths.Full, "Во всю ширину"),
        (FormItemWidths.Half, "Половина"),
        (FormItemWidths.Third, "Треть"),
    ];

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_source, Definition)) return;

        _source = Definition;
        Rebuild();
    }

    //=====================================
    // дерево

    void Rebuild()
    {
        _rows.Clear();

        if (Definition is not { } definition)
        {
            _zones = [];
            return;
        }

        _zones = definition.Zones.Count > 0
            ? definition.Zones.ToList()
            : definition.Items.Select(i => i.Zone ?? "")
                              .Distinct()
                              .Select(zone => new FormZoneDescriptor { Key = zone, Title = zone })
                              .ToList();

        var fallbackZone = _zones.FirstOrDefault()?.Key ?? "";
        foreach (var item in definition.Items)
            _rows.Add(Row(item, null, fallbackZone));

        if (_zones.All(z => z.Key != NewSectionZone))
            NewSectionZone = fallbackZone;
    }

    static LayoutRow Row(FormItem item, LayoutRow? parent, string fallbackZone)
    {
        var row = new LayoutRow
        {
            Key = item.Key,
            Kind = item.Kind,
            Field = item.Field,
            Zone = item.Zone ?? parent?.Zone ?? fallbackZone,
            Title = item.Title,
            Visible = item.Visible,
            Width = item.Width,
            Collapsed = item.Collapsed,
            Parent = parent,
        };

        foreach (var child in item.Items)
            row.Children.Add(Row(child, row, row.Zone));

        return row;
    }

    public IEnumerable<LayoutRow> RowsOf(string zone) => _rows.Where(row => row.Zone == zone);

    public string ZoneTitle(string key) => _zones.FirstOrDefault(zone => zone.Key == key)?.Title ?? key;

    /// <summary>Заголовок строки: переопределение раскладки, затем ключ ресурса, затем заголовок поля</summary>
    public string DisplayTitle(LayoutRow row)
    {
        if (!string.IsNullOrWhiteSpace(row.Title)) return row.Title;
        if (row.Field is not { } field) return row.Key;

        return string.IsNullOrEmpty(field.TitleKey) ? field.Title : L[field.TitleKey];
    }

    List<LayoutRow> ListOf(LayoutRow row) => row.Parent?.Children ?? _rows;

    //=====================================
    // изменения

    public Task EmitAsync() => ValueChanged.InvokeAsync(new FormLayoutSettings { Items = _rows.Select(ToItem).ToList() });

    static FormItem ToItem(LayoutRow row) => new()
    {
        Kind = row.Kind,
        Key = row.Key,
        // зона хранится только у корневых элементов: дети секции живут в её зоне
        Zone = row.Parent is null ? row.Zone : null,
        Title = string.IsNullOrWhiteSpace(row.Title) ? null : row.Title,
        Visible = row.Visible,
        Width = row.Width,
        Collapsed = row.Collapsed,
        Items = row.Children.Select(ToItem).ToList(),
    };

    public bool CanMove(LayoutRow row, int delta)
    {
        var list = ListOf(row);
        var index = list.IndexOf(row);
        if (index < 0) return false;

        // корневые строки идут одним списком с атрибутом зоны: сосед — следующая строка той же зоны
        return row.Parent is null
            ? delta < 0
                ? list.Take(index).Any(other => other.Zone == row.Zone)
                : list.Skip(index + 1).Any(other => other.Zone == row.Zone)
            : index + delta >= 0 && index + delta < list.Count;
    }

    public async Task MoveAsync(LayoutRow row, int delta)
    {
        var list = ListOf(row);
        var index = list.IndexOf(row);
        if (index < 0) return;

        if (row.Parent is null)
        {
            for (var i = index + delta; i >= 0 && i < list.Count; i += delta)
            {
                if (list[i].Zone != row.Zone) continue;

                (list[index], list[i]) = (list[i], list[index]);
                await EmitAsync();
                return;
            }

            return;
        }

        var target = index + delta;
        if (target < 0 || target >= list.Count) return;

        (list[index], list[target]) = (list[target], list[index]);
        await EmitAsync();
    }

    public async Task SetZoneAsync(LayoutRow row, string zone)
    {
        if (string.IsNullOrEmpty(zone) || row.Zone == zone) return;

        row.Zone = zone;
        // дети секции живут в её зоне: у них зона в раскладке не хранится, но строки дизайнера её знают
        foreach (var child in row.Children)
            child.Zone = zone;

        await EmitAsync();
    }

    public async Task SetWidthAsync(LayoutRow row, string width)
    {
        row.Width = string.IsNullOrEmpty(width) ? null : width;
        await EmitAsync();
    }

    public async Task AddSectionAsync()
    {
        var zone = _zones.Any(z => z.Key == NewSectionZone) ? NewSectionZone : _zones.FirstOrDefault()?.Key;
        if (zone is null) return;

        _rows.Add(new LayoutRow
        {
            Key = NewSectionKey(),
            Kind = FormItemKinds.Section,
            Zone = zone,
            Title = "Секция",
        });

        await EmitAsync();
    }

    /// <summary>Удаляет секцию, оставляя её поля в зоне на месте секции</summary>
    public async Task RemoveSectionAsync(LayoutRow section)
    {
        var index = _rows.IndexOf(section);
        if (index < 0) return;

        var children = section.Children.ToList();
        foreach (var child in children)
        {
            child.Parent = null;
            child.Zone = section.Zone;
        }

        section.Children.Clear();
        _rows.RemoveAt(index);
        _rows.InsertRange(index, children);

        await EmitAsync();
    }

    /// <summary>Поля зоны, которые можно забрать в секцию</summary>
    public IEnumerable<LayoutRow> SectionFieldCandidates(LayoutRow section)
        => _rows.Where(row => row.Parent is null && !row.IsSection && row.Zone == section.Zone);

    public async Task MoveIntoSectionAsync(LayoutRow section, string fieldKey)
    {
        var row = _rows.FirstOrDefault(other => other.Key == fieldKey && other.Parent is null && !other.IsSection);
        if (row is null) return;

        _rows.Remove(row);
        row.Parent = section;
        section.Children.Add(row);

        await EmitAsync();
    }

    public async Task MoveOutOfSectionAsync(LayoutRow row)
    {
        if (row.Parent is not { } section) return;

        section.Children.Remove(row);
        row.Parent = null;
        row.Zone = section.Zone;

        var index = _rows.IndexOf(section);
        _rows.Insert(index < 0 ? _rows.Count : index + 1, row);

        await EmitAsync();
    }

    public async Task ResetAsync()
    {
        await ValueChanged.InvokeAsync(null);
        await OnResetRequested.InvokeAsync();
    }

    string NewSectionKey()
    {
        var taken = _rows.Select(row => row.Key).Concat(_rows.SelectMany(row => row.Children.Select(child => child.Key))).ToHashSet();

        string key;
        do
        {
            key = "section-" + Guid.NewGuid().ToString("N")[..8];
        }
        while (taken.Contains(key));

        return key;
    }

    /// <summary>Строка элемента раскладки: то, что правит дизайнер</summary>
    public sealed class LayoutRow
    {
        public required string Key { get; init; }

        public string Kind { get; init; } = FormItemKinds.Field;

        /// <summary>Дескриптор поля провайдера (у секции отсутствует); в раскладку не сохраняется</summary>
        public FormFieldDescriptor? Field { get; init; }

        public LayoutRow? Parent { get; set; }

        public string Zone { get; set; } = "";

        /// <summary>Секция — заголовок; поле — переопределение заголовка (пусто = заголовок провайдера)</summary>
        public string? Title { get; set; }

        public bool Visible { get; set; } = true;

        public string? Width { get; set; }

        public bool Collapsed { get; set; }

        public List<LayoutRow> Children { get; set; } = [];

        public bool IsSection => Kind == FormItemKinds.Section;
    }
}
