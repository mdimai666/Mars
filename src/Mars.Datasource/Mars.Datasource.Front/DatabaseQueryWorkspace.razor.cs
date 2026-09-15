using Mars.Admin.Framework.Components;
using Mars.Admin.Framework.Services;
using Mars.Core.Extensions;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Dto;
using Mars.Datasource.Front.Services;
using MarsCodeEditor2;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Datasource.Front;

public partial class DatabaseQueryWorkspace
{
    [Inject] IDatasourceServiceClient service { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IAIToolAppService _aiTool { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;

    bool Busy;

    string _dataSourceConfigSlug = DatasourceConfig.DefaultSlug;

    [Parameter]
    public string DataSourceConfigSlug
    {
        get => string.IsNullOrWhiteSpace(_dataSourceConfigSlug) ? DatasourceConfig.DefaultSlug : _dataSourceConfigSlug;
        set
        {
            if (_dataSourceConfigSlug != value)
            {
                _dataSourceConfigSlug = value;
                _ = LoadAsync();
            }
        }
    }

    QDatabaseStructureResponse? database;

    QTableResponse? selTable = null;

    string?[][]? raw => res?.Rows;

    QueryResultDto? res = null;

    const int MaxRows = 500;

    bool loadingQuery = false;

    string? errorMessage;

    CodeEditor2? _editor = default!;

    IReadOnlyCollection<SelectDatasourceDto> listDatasources = [];

    string thisurl = "";

    /// <summary>Абсолютный адрес от base path: относительный ломается на вложенных маршрутах.</summary>
    string datasourceConfigUrl => $"{nav.BaseUri}datasource/config";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        thisurl = new Uri(nav.Uri).LocalPath;
        _ = LoadAsync();
    }

    async Task LoadAsync()
    {
        Busy = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            database = await service.DatabaseStructure(DataSourceConfigSlug);
            listDatasources = await service.ListSelectDatasource();
        }
        catch (Exception ex)
        {
            database = null;
            errorMessage = ex.Message;
        }
        finally
        {
            Busy = false;
            StateHasChanged();
        }
    }

    async Task OnClickTable(QTableResponse table)
    {
        selTable = table;

        await Task.Delay(100);

        _editor?.SetValue(GetSelectRowsQuery(table.TableName));
    }

    string GetSelectRowsQuery(string tableName)
    {
        var d = listDatasources.FirstOrDefault(s => s.Slug == DataSourceConfigSlug);
        var q = d?.EscapeQuotationMark ?? '"';
        if (d is not null)
        {
            if (d.Driver == "mssql") return $"SELECT TOP 20 * FROM {q}{tableName}{q}\n";
        }
        return $"SELECT * FROM {q}{tableName}{q}\nLIMIT 20";
    }

    async Task ClickQuery()
    {
        if (selTable is null) return;

        loadingQuery = true;
        StateHasChanged();

        try
        {
            _ = WaitHelper.WaitForNotNull(() => _editor, 2000);

            string sql = await _editor!.GetValue();

            if (string.IsNullOrWhiteSpace(sql))
            {
                _ = _messageService.Error("SQL query is empty!");
                return;
            }

            if (SqlSafety.IsDestructive(sql) && !await ConfirmDestructiveAsync(sql))
            {
                return;
            }

            res = await service.Query(DataSourceConfigSlug, new SqlRequest { Sql = sql, MaxRows = MaxRows });
        }
        catch (Exception ex)
        {
            res = new QueryResultDto { Ok = false, Message = ex.Message };
        }
        finally
        {
            loadingQuery = false;
            StateHasChanged();
        }
    }

    async Task<bool> ConfirmDestructiveAsync(string sql)
    {
        var dialog = await _dialogService.ShowDialogAsync<DeleteConfirmationDialog>(
            (MarkupString)$"Запрос <b>{SqlSafety.FirstWord(sql)}</b> изменяет данные или структуру базы. Выполнить?",
            new DialogParameters
            {
                Title = "Подтверждение запроса",
                Modal = true,
                PreventDismissOnOverlayClick = false,
            });

        var result = await dialog.Result;

        return !result.Cancelled;
    }

    async Task ShowRecords(QTableResponse table)
    {
        selTable = table;
        await Task.Delay(100);

        await ClickQuery();
    }
}
