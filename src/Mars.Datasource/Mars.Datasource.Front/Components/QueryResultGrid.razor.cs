using System.Text.Json;
using System.Text.Json.Nodes;
using Mars.Admin.Framework.Interfaces;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Dto;
using Mars.Datasource.Front.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Datasource.Front.Components;

public partial class QueryResultGrid
{
    [Inject] IDatasourceServiceClient service { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;

    [Parameter, EditorRequired] public QueryResultDto Result { get; set; } = default!;
    [Parameter, EditorRequired] public QueryTab Tab { get; set; } = default!;
    [Parameter] public string Slug { get; set; } = DatasourceConfig.DefaultSlug;
    [Parameter] public SelectDatasourceDto? Source { get; set; }

    /// <summary>Вызывается после успешного сохранения правок — рабочая область перечитывает данные.</summary>
    [Parameter] public EventCallback OnSaved { get; set; }

    (int Row, string Column)? _editCell;
    string? _editValue;
    ElementReference _editInput;
    bool _focusNeeded;

    protected override void OnParametersSet()
    {
        // Результат переехал в другую вкладку — незавершённую правку не тащим.
        _editCell = null;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_focusNeeded) return;

        _focusNeeded = false;
        await _editInput.FocusAsync();
    }

    bool IsEditing((int Row, string Column) cell)
        => Tab.CanEdit && _editCell == cell;

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

        var original = GetValue(cell.Row, cell.Column);

        if (_editValue == original)
        {
            Tab.Changes.Remove(cell);
        }
        else
        {
            Tab.Changes[cell] = _editValue;
        }

        StateHasChanged();
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
            var response = await service.NonQuery(Slug, new SqlRequest { Sql = plan.Sql, Parameters = plan.Parameters });

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
        if (Tab.Table is null) return [];

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
                Tab.Table.TableSchema.SchemaName,
                Tab.Table.TableName,
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
        var start = Source?.QuoteStart ?? '"';
        var end = Source?.QuoteEnd ?? '"';

        return name => $"{start}{name}{end}";
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

            foreach (var row in Result.Rows)
            {
                JsonObject obj = new();

                for (var i = 0; i < Result.Columns.Length && i < row.Length; i++)
                {
                    var column = Result.Columns[i];
                    var value = row[i];

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
