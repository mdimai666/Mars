using Mars.Contracts.Resources;
using Mars.Forms.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Дизайнер раскладки формы: плоский список по зонам — порядок, зона, видимость, ширина
/// и маркеры секций (поля идут за маркером до следующего — как Tab в ACF).
/// Параметры самих полей (правила, редактор) здесь не редактируются: они живут в настройках
/// владельца формы и правятся на странице типа. Наружу уходит только раскладка
/// (<see cref="FormLayoutSettings"/>): дескрипторы не хранятся, их каждый раз отдаёт провайдер.
/// </summary>
public partial class FormLayoutEditor
{
    [Inject] IStringLocalizer<AppRes> L { get; set; } = default!;

    /// <summary>Действующее определение провайдера: уже с применённой сохранённой раскладкой</summary>
    [Parameter] public FormDefinition? Definition { get; set; }

    /// <summary>Раскладка после каждого изменения; null — сброс к раскладке провайдера</summary>
    [Parameter] public EventCallback<FormLayoutSettings?> ValueChanged { get; set; }

    /// <summary>
    /// Просьба сбросить раскладку: хост подменяет <see cref="Definition"/> на раскладку по умолчанию
    /// (дизайнер не знает порядка провайдера — определение собирает сервер).
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
        {
            _rows.Add(new LayoutRow
            {
                Key = item.Key,
                Field = item.Field,
                Zone = string.IsNullOrEmpty(item.Zone) ? fallbackZone : item.Zone,
                Title = item.Title,
                SectionTitle = item.SectionTitle,
                Visible = item.Visible,
                Width = item.Width,
            });
        }

        if (_zones.All(z => z.Key != NewSectionZone))
            NewSectionZone = fallbackZone;
    }

    /// <summary>Строки зоны в порядке отображения; Depth = 1 у полей, идущих за маркером секции</summary>
    public IEnumerable<(LayoutRow Row, int Depth)> EntriesOf(string zone)
    {
        var depth = 0;

        foreach (var row in _rows.Where(r => r.Zone == zone))
        {
            yield return (row, depth);

            if (row.IsSection) depth = 1;
        }
    }

    public string ZoneTitle(string key) => _zones.FirstOrDefault(zone => zone.Key == key)?.Title ?? key;

    /// <summary>Заголовок строки: переопределение раскладки, затем ключ ресурса, затем заголовок поля</summary>
    public string DisplayTitle(LayoutRow row)
    {
        if (!string.IsNullOrWhiteSpace(row.Title)) return row.Title;
        if (row.Field is not { } field) return row.Key;

        return string.IsNullOrEmpty(field.TitleKey) ? field.Title : L[field.TitleKey];
    }

    //=====================================
    // изменения

    public Task EmitAsync()
        => ValueChanged.InvokeAsync(new FormLayoutSettings { Items = _rows.Select(ToItem).ToList() });

    static FormItem ToItem(LayoutRow row) => new()
    {
        Key = row.Key,
        Zone = row.Zone,
        Title = string.IsNullOrWhiteSpace(row.Title) ? null : row.Title,
        SectionTitle = row.SectionTitle,
        Visible = row.Visible,
        Width = row.Width,
    };

    /// <summary>Сосед по зоне: раскладка плоская, порядок строк и есть порядок отображения</summary>
    public bool CanMove(LayoutRow row, int delta)
    {
        var zone = _rows.Where(r => r.Zone == row.Zone).ToList();
        var index = zone.IndexOf(row);
        var target = index + delta;

        return index >= 0 && target >= 0 && target < zone.Count;
    }

    public async Task MoveAsync(LayoutRow row, int delta)
    {
        var zone = _rows.Where(r => r.Zone == row.Zone).ToList();
        var index = zone.IndexOf(row);
        var target = index + delta;
        if (index < 0 || target < 0 || target >= zone.Count) return;

        var other = zone[target];
        var left = _rows.IndexOf(row);
        var right = _rows.IndexOf(other);

        (_rows[left], _rows[right]) = (_rows[right], _rows[left]);

        await EmitAsync();
    }

    public async Task SetZoneAsync(LayoutRow row, string zone)
    {
        if (string.IsNullOrEmpty(zone) || row.Zone == zone) return;

        row.Zone = zone;
        await EmitAsync();
    }

    public async Task SetWidthAsync(LayoutRow row, string width)
    {
        row.Width = string.IsNullOrEmpty(width) ? null : width;
        await EmitAsync();
    }

    public async Task SetSectionTitleAsync(LayoutRow row, string title)
    {
        row.SectionTitle = title;
        await EmitAsync();
    }

    /// <summary>Добавляет маркер секции в конец выбранной зоны</summary>
    public async Task AddSectionAsync()
    {
        var zone = _zones.Any(z => z.Key == NewSectionZone) ? NewSectionZone : _zones.FirstOrDefault()?.Key;
        if (zone is null) return;

        _rows.Add(new LayoutRow
        {
            Key = NewSectionKey(),
            Zone = zone,
            SectionTitle = "Секция",
        });

        await EmitAsync();
    }

    /// <summary>Убирает маркер секции: её поля остаются на своих местах</summary>
    public async Task RemoveSectionAsync(LayoutRow section)
    {
        if (!_rows.Remove(section)) return;

        await EmitAsync();
    }

    public async Task ResetAsync()
    {
        await ValueChanged.InvokeAsync(null);
        await OnResetRequested.InvokeAsync();
    }

    string NewSectionKey()
    {
        var taken = _rows.Select(row => row.Key).ToHashSet();

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

        /// <summary>Дескриптор поля провайдера (у маркера секции отсутствует); в раскладку не сохраняется</summary>
        public FormFieldDescriptor? Field { get; init; }

        public string Zone { get; set; } = "";

        /// <summary>Маркер секции: заголовок группы; поля идут за ним до следующего маркера</summary>
        public string? SectionTitle { get; set; }

        /// <summary>Переопределение заголовка поля (пусто = заголовок провайдера)</summary>
        public string? Title { get; set; }

        public bool Visible { get; set; } = true;

        public string? Width { get; set; }

        public bool IsSection => SectionTitle is not null;
    }
}
