using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Pages.PostsViews;

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
        var available = PostTypeGridColumns.Available(EnabledFeatures, MetaFields);

        _rows = PostTypeGridColumns.Merge(value?.Columns, available)
                                   .Select(c => new GridColumnRow(c, c.Visible))
                                   .ToList();

        _sortKey = value?.SortKey ?? "";
        _sortDescending = value?.SortDescending ?? false;
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

    sealed class GridColumnRow(PostTypeGridColumnInfo column, bool visible)
    {
        public string Key => column.Key;
        public string Title => column.Title;
        public bool IsSystem => column.IsSystem;
        public bool Visible { get; set; } = visible;
    }
}
