using Mars.Admin.Framework.Components;
using Mars.Admin.Framework.Services;
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
    string? errorMessage;
    string tableFilter = "";

    string _slug = DatasourceConfig.DefaultSlug;

    [Parameter]
    public string DataSourceConfigSlug
    {
        get => string.IsNullOrWhiteSpace(_slug) ? DatasourceConfig.DefaultSlug : _slug;
        set
        {
            if (_slug != value)
            {
                _slug = value;
                _ = LoadAsync();
            }
        }
    }

    QDatabaseStructureResponse? database;
    IReadOnlyCollection<SelectDatasourceDto> listDatasources = [];

    List<QueryTab> tabs = [];
    string? activeTabId;

    QueryTab? activeTab => tabs.FirstOrDefault(t => t.Id == activeTabId) ?? tabs.FirstOrDefault();

    SelectDatasourceDto? source => listDatasources.FirstOrDefault(s => s.Slug == DataSourceConfigSlug);

    CodeEditor2? _editor;

    /// <summary>JS-редактор создаётся не в момент появления ссылки на компонент, а в OnInit —
    /// до этого SetValue/GetValue падают с «Couldn't find the editor with id».</summary>
    bool _editorReady;

    bool _editorNeedsSync;

    string datasourceConfigUrl => $"{nav.BaseUri}datasource/config";

    IEnumerable<QTableResponse> filteredTables => database is null
        ? []
        : (string.IsNullOrWhiteSpace(tableFilter)
            ? database.Tables
            : database.Tables.Where(t => t.TableName.Contains(tableFilter, StringComparison.OrdinalIgnoreCase)))
          .OrderBy(t => t.TableSchema.SchemaName)
          .ThenBy(t => t.TableName);

    bool hasMultipleSchemas => database is not null
        && database.Tables.Select(t => t.TableSchema.SchemaName).Distinct().Count() > 1;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        AddTab();
        _ = LoadAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_editorNeedsSync || !_editorReady) return;

        await SyncEditorAsync();
    }

    async Task OnEditorInitAsync()
    {
        _editorReady = true;

        await SyncEditorAsync();
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

    async Task RefreshStructureAsync()
    {
        Busy = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            database = await service.RefreshStructure(DataSourceConfigSlug);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            Busy = false;
            StateHasChanged();
        }
    }

    //=== вкладки запросов =====================================================

    QueryTab AddTab()
    {
        var tab = new QueryTab { Title = $"query {tabs.Count + 1}" };

        tabs.Add(tab);
        activeTabId = tab.Id;
        _editorNeedsSync = true;

        StateHasChanged();

        return tab;
    }

    async Task SelectTabAsync(QueryTab tab)
    {
        if (tab == activeTab) return;

        await RememberEditorSqlAsync();

        activeTabId = tab.Id;
        _editorNeedsSync = true;

        StateHasChanged();
    }

    async Task CloseTabAsync(QueryTab tab)
    {
        await RememberEditorSqlAsync();

        tabs.Remove(tab);

        if (tabs.Count == 0)
        {
            AddTab();
            return;
        }

        activeTabId = tabs[0].Id;
        _editorNeedsSync = true;

        StateHasChanged();
    }

    /// <summary>Текст редактора принадлежит активной вкладке: перед переключением забираем его.</summary>
    async Task RememberEditorSqlAsync()
    {
        var tab = activeTab;
        if (!_editorReady || tab is null || _editor is null) return;

        var sql = await _editor.GetValue();
        if (!string.IsNullOrWhiteSpace(sql)) tab.Sql = sql;
    }

    async Task SyncEditorAsync()
    {
        if (!_editorReady || _editor is null || activeTab is null) return;

        _editorNeedsSync = false;
        await _editor.SetValue(activeTab.Sql);
    }

    async Task<string> ReadEditorSqlAsync()
    {
        if (!_editorReady || _editor is null) return activeTab?.Sql ?? "";

        return await _editor.GetValue();
    }

    //=== выполнение ===========================================================

    async Task RunActiveTabAsync()
    {
        var tab = activeTab;
        if (tab is null || tab.Loading) return;

        string sql = await ReadEditorSqlAsync();

        if (string.IsNullOrWhiteSpace(sql))
        {
            _ = _messageService.Error("SQL-запрос пуст");
            return;
        }

        if (SqlSafety.IsDestructive(sql) && !await ConfirmDestructiveAsync(sql)) return;

        tab.Sql = sql;
        tab.Title = tab.Table?.TableName ?? tab.Title;
        tab.Loading = true;
        tab.Error = null;

        StateHasChanged();

        try
        {
            var result = await service.Query(DataSourceConfigSlug, new SqlRequest { Sql = sql, MaxRows = tab.MaxRows });

            tab.Result = result;
            tab.Error = result.Ok ? null : result.Message;
        }
        catch (Exception ex)
        {
            tab.Result = null;
            tab.Error = ex.Message;
        }
        finally
        {
            tab.Loading = false;
            StateHasChanged();
        }
    }

    /// <summary>Запросить больше строк: серверный лимит растёт, SQL не меняется.</summary>
    async Task RunMoreAsync()
    {
        var tab = activeTab;
        if (tab is null) return;

        tab.MaxRows *= 5;

        await RunActiveTabAsync();
    }

    async Task OpenTableAsync(QTableResponse table)
    {
        var tab = activeTab ?? AddTab();

        tab.Table = table;
        tab.KeyColumns = table.Columns.Values
            .Where(c => c.IsKey == true)
            .Select(c => c.ColumnName)
            .ToList();
        tab.Title = table.TableName;
        tab.Sql = BuildBrowseSql(table);
        tab.Changes.Clear();
        tab.ShowJson = false;

        _editorNeedsSync = true;

        await SyncEditorAsync();
        await RunActiveTabAsync();
    }

    string BuildBrowseSql(QTableResponse table)
    {
        var quote = Quoter();
        var schema = table.TableSchema.SchemaName;

        var name = string.IsNullOrEmpty(schema)
            ? quote(table.TableName)
            : $"{quote(schema)}.{quote(table.TableName)}";

        var keys = table.Columns.Values
            .Where(c => c.IsKey == true)
            .OrderBy(c => c.ColumnOrdinal)
            .Select(c => quote(c.ColumnName))
            .ToList();

        var orderBy = keys.Count == 0 ? "" : $" ORDER BY {string.Join(", ", keys)}";

        return $"SELECT * FROM {name}{orderBy}";
    }

    Func<string, string> Quoter()
    {
        var start = source?.QuoteStart ?? '"';
        var end = source?.QuoteEnd ?? '"';

        return name => $"{start}{name}{end}";
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
}
