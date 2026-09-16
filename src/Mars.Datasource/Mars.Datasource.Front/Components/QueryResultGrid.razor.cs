using System.Text.Json;
using System.Text.Json.Nodes;
using Mars.Admin.Framework.Interfaces;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Front.Services;
using Mars.Datasource.Contracts.Models;
using MarsCodeEditor2;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Datasource.Front.Components;

public partial class QueryResultGrid
{
    [Inject] IDatasourceServiceClient service { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] MarsDatasourceFrontJsInterop jsInterop { get; set; } = default!;

    [Parameter, EditorRequired] public QueryResultDto Result { get; set; } = default!;
    [Parameter, EditorRequired] public QueryTab Tab { get; set; } = default!;
    [Parameter] public string Slug { get; set; } = DatasourceConfig.DefaultSlug;
    [Parameter] public SelectDatasourceDto? Source { get; set; }

    /// <summary>Вызывается после успешного сохранения правок — рабочая область перечитывает данные.</summary>
    [Parameter] public EventCallback OnSaved { get; set; }

    /// <summary>Длина начальных и конечных символов, которые видны у сжатого значения.</summary>
    const int MidHeadLength = 8;
    const int MidTailLength = 6;

    ElementReference _root;
    (int Row, string Column)? _editCell;
    string? _editValue;
    ElementReference _editInput;
    bool _focusNeeded;

    QueryResultDto? _guidDetectedFor;
    bool[] _guidColumns = [];

    protected override void OnParametersSet()
    {
        // Результат переехал в другую вкладку — незавершённую правку не тащим.
        _editCell = null;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Копирование выделения из грида отдаёт полные значения ячеек, а не «начало…конец».
            await jsInterop.RegisterFullValueCopy(_root);
        }

        if (!_focusNeeded) return;

        _focusNeeded = false;
        await _editInput.FocusAsync();
    }

    /// <summary>
    /// Класс цвета по категории типа — для значения ячейки и для подписи типа.
    /// Строковые без класса: их большинство, и в ячейке это основной текст (цвет по умолчанию), а не подпись.
    /// </summary>
    static string? KindClass(QColumnKind kind)
        => kind switch
        {
            QColumnKind.Number => "ds-type-number",
            QColumnKind.Boolean => "ds-type-bool",
            QColumnKind.DateTime => "ds-type-date",
            QColumnKind.Json => "ds-type-json",
            _ => null,
        };

    /// <summary>
    /// Начало и конец длинного значения без середины — для колонок с guid-ами: 36 символов
    /// распирают таблицу, а по краям значение всё ещё узнаваемо. `null` — показываем целиком.
    /// </summary>
    (string Head, string Tail)? MidParts(int columnIndex, string? value)
    {
        if (value is null || !IsGuidColumn(columnIndex)) return null;
        if (value.Length <= MidHeadLength + MidTailLength + 1) return null;

        return (value[..MidHeadLength], value[^MidTailLength..]);
    }

    bool IsGuidColumn(int columnIndex)
    {
        if (!ReferenceEquals(_guidDetectedFor, Result))
        {
            _guidDetectedFor = Result;
            _guidColumns = DetectGuidColumns();
        }

        return columnIndex >= 0 && columnIndex < _guidColumns.Length && _guidColumns[columnIndex];
    }

    bool[] DetectGuidColumns()
    {
        var flags = new bool[Result.Columns.Length];

        for (var i = 0; i < flags.Length; i++)
        {
            flags[i] = QColumnMapping.Kind(Result.Columns[i].DataTypeName) == QColumnKind.Guid
                || LooksLikeGuidColumn(i);
        }

        return flags;
    }

    /// <summary>Guid в char-колонке (MySQL, старые схемы) по типу не отличить — смотрим значения.</summary>
    bool LooksLikeGuidColumn(int columnIndex)
    {
        const int sampleSize = 5;

        var sampled = 0;
        var guids = 0;

        foreach (var row in Result.Rows)
        {
            if (sampled >= sampleSize) break;
            if (columnIndex >= row.Length) continue;

            var value = row[columnIndex];
            if (string.IsNullOrEmpty(value)) continue;

            sampled++;

            if (Guid.TryParse(value, out _)) guids++;
        }

        return sampled > 0 && guids == sampled;
    }

    /// <summary>
    /// «строк: 20 · всего 1234». Общее число есть только у просмотра объекта из дерева
    /// (`QueryTab.Total`), у произвольного запроса мы не знаем, что считать.
    /// </summary>
    string RowsSummary
    {
        get
        {
            var text = $"строк: {Result.Rows.Length}";

            if (Tab.Total is long total) text += $" из {total}";
            if (Tab.TotalNote is not null) text += $" ({Tab.TotalNote})";

            return text;
        }
    }

    bool IsEditing((int Row, string Column) cell)
        => Tab.CanEdit && _editCell == cell;

    /// <summary>Длинное значение правится в модалке: в ячейке (330px) его всё равно не видно.</summary>
    const int InlineEditMaxLength = 50;

    async Task StartCellEditAsync((int Row, string Column) cell, string? value, QColumnKind kind)
    {
        if (!Tab.CanEdit) return;

        if (value is not null && value.Length > InlineEditMaxLength)
        {
            await EditLongValueAsync(cell, value, kind);
            return;
        }

        StartEdit(cell, value);
    }

    async Task EditLongValueAsync((int Row, string Column) cell, string value, QColumnKind kind)
    {
        var dialog = await _dialogService.ShowDialogAsync<CellValueDialog>(
            new CellValueDialogContent(value, kind == QColumnKind.Json ? CodeEditor2.Language.json : "plaintext"),
            new DialogParameters
            {
                Title = $"Значение: {cell.Column}",
                Width = "min(960px, 90vw)",
                Modal = true,
                PreventDismissOnOverlayClick = true,
            });

        var result = await dialog.Result;
        if (result.Cancelled) return;

        ApplyEdit(cell, result.Data as string);
        StateHasChanged();
    }

    void StartEdit((int Row, string Column) cell, string? value)
    {
        if (!Tab.CanEdit) return;
        if (_editCell == cell) return;

        _editCell = cell;
        _editValue = value;
        _focusNeeded = true;
        StateHasChanged();
    }

    async Task OnEditKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            CommitEdit();
        }
        else if (e.Key == "Escape")
        {
            _editCell = null;
            StateHasChanged();
            await Task.CompletedTask;
        }
    }

    void CommitEdit()
    {
        if (_editCell is not { } cell) return;

        _editCell = null;
        ApplyEdit(cell, _editValue);
        StateHasChanged();
    }

    /// <summary>Значение ложится в несохранённые правки; равное исходному — снимает правку.</summary>
    void ApplyEdit((int Row, string Column) cell, string? value)
    {
        if (value == GetValue(cell.Row, cell.Column))
        {
            Tab.Changes.Remove(cell);
        }
        else
        {
            Tab.Changes[cell] = value;
        }
    }

    void DiscardChanges()
    {
        Tab.Changes.Clear();
        StateHasChanged();
    }

    async Task SaveAsync()
    {
        var editedRows = Tab.Changes.Keys.Select(k => k.Row).Distinct().Count();
        var plans = BuildPlans();

        if (plans.Count == 0 || plans.Count != editedRows)
        {
            _ = _messageService.Error("Не удалось собрать UPDATE: не хватает значений первичного ключа");
            return;
        }

        var dialog = await _dialogService.ShowDialogAsync<SqlPreviewDialog>(plans, new DialogParameters
        {
            Title = "Изменение данных",
            Modal = true,
            PreventDismissOnOverlayClick = false,
        });

        var result = await dialog.Result;
        if (result.Cancelled) return;

        var affected = 0;

        foreach (var plan in plans)
        {
            var response = await service.NonQuery(Slug, new DatasourceRequest { Query = plan.Sql, Parameters = plan.Parameters });

            if (!response.Ok)
            {
                _ = _messageService.Error(response.Message);
                return;
            }

            affected += response.RowsAffected;
        }

        Tab.Changes.Clear();
        _ = _messageService.Success($"Обновлено строк: {affected}");

        await OnSaved.InvokeAsync();
    }

    List<SqlUpdatePlan> BuildPlans()
    {
        if (Tab.Object is null) return [];

        var quote = Quoter();
        List<SqlUpdatePlan> plans = [];

        foreach (var rowGroup in Tab.Changes.GroupBy(c => c.Key.Row))
        {
            Dictionary<string, string?> keyValues = [];

            foreach (var keyColumn in Tab.KeyColumns)
            {
                var index = ColumnIndex(keyColumn);
                if (index < 0) continue;

                keyValues[keyColumn] = GetValue(rowGroup.Key, keyColumn);
            }

            var changes = rowGroup.ToDictionary(c => c.Key.Column, c => c.Value);

            var plan = RowUpdateBuilder.Build(
                Tab.Schema,
                Tab.Object.Name,
                quote,
                Tab.KeyColumns,
                keyValues,
                changes);

            if (plan is not null) plans.Add(plan);
        }

        return plans;
    }

    Func<string, string> Quoter()
    {
        var dialect = SqlDialectMapping.Dialect(Source?.Driver);

        return name => SqlDialectMapping.Quote(dialect, name);
    }

    int ColumnIndex(string columnName)
        => Array.FindIndex(Result.Columns, c => c.Name == columnName);

    string? GetValue(int rowIndex, string columnName)
    {
        var index = ColumnIndex(columnName);
        if (index < 0) return null;
        if (rowIndex < 0 || rowIndex >= Result.Rows.Length) return null;

        var row = Result.Rows[rowIndex];

        return index < row.Length ? row[index] : null;
    }

    /// <summary>
    /// Результат как JSON: json-колонки разворачиваются вложенными объектами,
    /// остальные значения остаются строками.
    /// </summary>
    string? resultJsonText
    {
        get
        {
            if (!Result.Ok) return null;

            JsonArray array = new();

            for (var rowIndex = 0; rowIndex < Result.Rows.Length; rowIndex++)
            {
                var row = Result.Rows[rowIndex];
                JsonObject obj = new();

                for (var i = 0; i < Result.Columns.Length && i < row.Length; i++)
                {
                    var column = Result.Columns[i];
                    var value = Tab.Changes.TryGetValue((rowIndex, column.Name), out var pending) ? pending : row[i];

                    if (value is null)
                    {
                        obj[column.Name] = null;
                    }
                    else if (column.IsJson && TryParseJson(value) is JsonNode node)
                    {
                        obj[column.Name] = node;
                    }
                    else
                    {
                        obj[column.Name] = JsonValue.Create(value);
                    }
                }

                array.Add(obj);
            }

            return array.ToJsonString();
        }
    }

    static JsonNode? TryParseJson(string value)
    {
        try
        {
            return JsonNode.Parse(value);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
