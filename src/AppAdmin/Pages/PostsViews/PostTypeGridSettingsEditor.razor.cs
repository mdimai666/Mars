using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Resources;
using Microsoft.AspNetCore.Components;

namespace AppAdmin.Pages.PostsViews;

/// <summary>
/// Редактор настроек колонок грида постов типа: видимость, порядок, сортировка по умолчанию.
/// Используется в диалоге на списке постов и на странице презентации типа.
/// </summary>
public partial class PostTypeGridSettingsEditor
{
    [Parameter] public PostTypeGridSettings? Value { get; set; }
    [Parameter] public EventCallback<PostTypeGridSettings?> ValueChanged { get; set; }

    /// <summary>Фичи типа — включают колонки «категории»/«статус»</summary>
    [Parameter] public IReadOnlyCollection<string> EnabledFeatures { get; set; } = [];

    /// <summary>Мета-поля типа — источники мета-колонок</summary>
    [Parameter] public IReadOnlyCollection<MetaFieldDetailResponse>? MetaFields { get; set; }

    List<GridColumnRow> _rows = [];
    string _sortKey = "";
    bool _sortDescending;
    PostTypeGridSettings? _lastValue;
    bool _initialized;

    protected override void OnParametersSet()
    {
        if (_initialized && ReferenceEquals(_lastValue, Value)) return;
        _initialized = true;
        _lastValue = Value;
        RebuildFrom(Value);
    }

    void RebuildFrom(PostTypeGridSettings? value)
    {
        var known = BuildKnownColumns();
        _rows = [];

        foreach (var conf in value?.Columns ?? [])
        {
            var knownCol = known.FirstOrDefault(k => k.Key == conf.Key);
            if (knownCol is null) continue;
            _rows.Add(new GridColumnRow(knownCol.Key, knownCol.Title, knownCol.IsBase, conf.Visible));
            known.Remove(knownCol);
        }


        // колонки, которых нет в настройке, — в конце видимыми
        _rows.AddRange(known.Select(k => new GridColumnRow(k.Key, k.Title, k.IsBase, true)));

        _sortKey = value?.SortKey ?? "";
        _sortDescending = value?.SortDescending ?? false;
    }

    List<KnownColumn> BuildKnownColumns()
    {
        var list = new List<KnownColumn>
        {
            new(PostTypeGridConstants.Title, AppRes.Title, true),
        };

        if (EnabledFeatures.Contains(PostTypeConstants.Features.Category))
            list.Add(new KnownColumn(PostTypeGridConstants.Categories, AppRes.Categories, true));

        if (EnabledFeatures.Contains(PostTypeConstants.Features.Status))
            list.Add(new KnownColumn(PostTypeGridConstants.Status, AppRes.Status, true));

        list.Add(new KnownColumn(PostTypeGridConstants.Author, AppRes.Author, true));
        list.Add(new KnownColumn(PostTypeGridConstants.CreatedAt, AppRes.CreatedAt, true));

        foreach (var field in MetaFields ?? [])
        {
            // плоскому гриду не подходят многовариантные и вычислимые поля
            if (field.Type is MetaFieldType.Query or MetaFieldType.SelectMany) continue;
            list.Add(new KnownColumn(field.Key, field.Title, false));
        }

        return list;
    }

    PostTypeGridSettings BuildSettings()
        => new()
        {
            Columns = _rows.Select(r => new PostTypeGridColumn { Key = r.Key, Visible = r.Visible }).ToList(),
            SortKey = string.IsNullOrEmpty(_sortKey) ? null : _sortKey,
            SortDescending = _sortDescending,
        };

    async Task EmitAsync()
    {
        var settings = BuildSettings();
        _lastValue = settings;
        await ValueChanged.InvokeAsync(settings);
    }

    async Task ToggleAsync(GridColumnRow row, bool visible)
    {
        row.Visible = visible;
        await EmitAsync();
    }

    async Task MoveAsync(GridColumnRow row, int delta)
    {
        var index = _rows.IndexOf(row);
        var target = index + delta;
        if (index < 0 || target < 0 || target >= _rows.Count) return;

        (_rows[index], _rows[target]) = (_rows[target], _rows[index]);
        await EmitAsync();
    }

    async Task ResetAsync()
    {
        _lastValue = null;
        RebuildFrom(null);
        await ValueChanged.InvokeAsync(null);
    }

    sealed record KnownColumn(string Key, string Title, bool IsBase);

    sealed class GridColumnRow(string key, string title, bool isBase, bool visible)
    {
        public string Key { get; } = key;
        public string Title { get; } = title;
        public bool IsBase { get; } = isBase;
        public bool Visible { get; set; } = visible;
    }
}
