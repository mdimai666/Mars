using Mars.Admin.Framework.Components;
using Mars.Admin.Framework.Services;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Dto;
using Mars.Datasource.Front.Components;
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

    /// <summary>Сколько строк открывать при клике по объекту: смотрим данные, а не выгружаем таблицу.</summary>
    const int DefaultBrowseLimit = 50;

    /// <summary>Сколько секунд ждём `COUNT(*)`: отменить запрос из UI пока нельзя, лучше честно сказать «не сосчитали».</summary>
    const int TotalCountTimeoutSec = 5;

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
            StartTotalCount(tab, sql, result);
        }
        catch (Exception ex)
        {
            tab.Result = null;
            tab.Error = ex.Message;
            tab.Total = null;
            tab.TotalNote = null;
        }
        finally
        {
            tab.Loading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Просим больше строк. У просмотра объекта лимит стоит в самом SQL — растим его и пересобираем запрос;
    /// у произвольного запроса ограничение серверное, поэтому растём его.
    /// </summary>
    async Task RunMoreAsync()
    {
        var tab = activeTab;
        if (tab is null) return;

        if (tab.IsBrowse)
        {
            tab.BrowseLimit *= 5;
            tab.BrowseSql = BuildBrowseSql(tab.Table!, tab.BrowseLimit);
            tab.Sql = tab.BrowseSql;

            EnsureBrowseCap(tab);
            _editorNeedsSync = true;

            await SyncEditorAsync();
        }
        else
        {
            tab.MaxRows *= 5;
        }

        await RunActiveTabAsync();
    }

    /// <summary>
    /// «Всего N» показываем только для нашего просмотра объекта: у произвольного запроса непонятно,
    /// что считать. Меньше лимита строк — количество известно и так; ровно лимит — считаем в фоне,
    /// чтобы не задерживать показ строк.
    /// </summary>
    void StartTotalCount(QueryTab tab, string sql, QueryResultDto result)
    {
        tab.Total = null;
        tab.TotalNote = null;

        if (!result.Ok || tab.BrowseSql is null || sql != tab.BrowseSql) return;

        if (result.Rows.Length < tab.BrowseLimit)
        {
            tab.Total = result.Rows.Length;
            return;
        }

        _ = CountTotalAsync(tab, result);
    }

    /// <summary>
    /// `COUNT(*)` по объекту. Ограничен по времени: считать большое число строк можно долго, а отменить
    /// запрос из UI пока нельзя — лучше честно сказать «не сосчитали», чем держать вкладку занятой.
    /// </summary>
    async Task CountTotalAsync(QueryTab tab, QueryResultDto result)
    {
        tab.TotalNote = "считаем всего…";
        StateHasChanged();

        try
        {
            var count = await service.Query(DataSourceConfigSlug, new SqlRequest
            {
                Sql = BuildCountSql(tab.Table!),
                MaxRows = 1,
                TimeoutSec = TotalCountTimeoutSec,
            });

            // Пока считали, вкладку могли перезапросить: тогда счёт уже не про текущий результат.
            if (!ReferenceEquals(tab.Result, result)) return;

            if (count.Ok)
            {
                tab.Total = ParseTotal(count);
                tab.TotalNote = null;
            }
            else
            {
                tab.TotalNote = $"всего не сосчитали за {TotalCountTimeoutSec} с";
            }
        }
        catch (Exception)
        {
            // Счёт — только украшение шапки: ошибку самого запроса из-за него не показываем.
        }
        finally
        {
            if (ReferenceEquals(tab.Result, result)) StateHasChanged();
        }
    }

    static long? ParseTotal(QueryResultDto result)
        => result.Rows.Length > 0
            && result.Rows[0].Length > 0
            && long.TryParse(result.Rows[0][0], out var total)
                ? total
                : null;

    async Task OpenTableAsync(QTableResponse table)
    {
        var tab = activeTab ?? AddTab();

        // Открыли другой объект — «изменяем вьюху» больше не про него.
        _viewSource = null;

        tab.Table = table;
        tab.KeyColumns = table.Columns.Values
            .Where(c => c.IsKey == true)
            .Select(c => c.ColumnName)
            .ToList();
        tab.Title = table.TableName;
        tab.BrowseLimit = DefaultBrowseLimit;
        tab.BrowseSql = BuildBrowseSql(table, tab.BrowseLimit);
        tab.Sql = tab.BrowseSql;
        tab.Total = null;
        tab.TotalNote = null;
        tab.Changes.Clear();
        tab.ShowJson = false;

        EnsureBrowseCap(tab);
        _editorNeedsSync = true;

        await SyncEditorAsync();
        await RunActiveTabAsync();
    }

    /// <summary>
    /// Просмотр объекта: лимит строк ставится в сам SQL, поэтому серверный предел должен быть выше —
    /// иначе «Показать больше» за серверный предел ничего не покажет.
    /// </summary>
    void EnsureBrowseCap(QueryTab tab)
        => tab.MaxRows = Math.Max(tab.MaxRows, tab.BrowseLimit + 1);

    string BuildBrowseSql(QTableResponse table, int limit)
        => BrowseSqlBuilder.Build(
            SqlDialectMapping.Dialect(source?.Driver),
            table.TableSchema.SchemaName,
            table.TableName,
            table.Columns.Values
                .Where(c => c.IsKey == true)
                .OrderBy(c => c.ColumnOrdinal)
                .Select(c => c.ColumnName)
                .ToList(),
            limit);

    string BuildCountSql(QTableResponse table)
        => BrowseSqlBuilder.Count(SqlDialectMapping.Dialect(source?.Driver), table.TableSchema.SchemaName, table.TableName);

    Func<string, string> Quoter()
    {
        var start = source?.QuoteStart ?? '"';
        var end = source?.QuoteEnd ?? '"';

        return name => $"{start}{name}{end}";
    }

    //=== вьюхи ================================================================

    /// <summary>
    /// Вьюха, определение которой загружено в редактор: диалог вьюхи предзаполняется ею
    /// (сценарий «изменить существующую»).
    /// </summary>
    QTableResponse? _viewSource;

    /// <summary>
    /// Активный объект, если это обычная вьюха. Матвьюхи — вне этой фазы: у них другой DDL,
    /// и кнопки, которые на них падают, показывать не стоит.
    /// </summary>
    QTableResponse? ViewObject()
        => activeTab?.Table is { } table && table.TableSchema.Kind == QTableKind.View ? table : null;

    async Task CreateViewAsync()
    {
        var content = new CreateViewDialogContent(
            SqlDialectMapping.Dialect(source?.Driver),
            Schemas(),
            await ReadEditorSqlAsync(),
            _viewSource?.TableSchema.SchemaName ?? activeTab?.Table?.TableSchema.SchemaName,
            _viewSource?.TableName,
            _viewSource is not null);

        var dialog = await _dialogService.ShowDialogAsync<CreateViewDialog>(content, new DialogParameters
        {
            Title = _viewSource is null ? "Новая вьюха" : $"Вьюха: {DisplayName(_viewSource)}",
            Width = "min(760px, 95vw)",
            Modal = true,
            PreventDismissOnOverlayClick = true,
        });

        var result = await dialog.Result;

        if (result.Cancelled || result.Data is not ViewDdlRequest request) return;

        if (!await ExecuteViewDdlAsync(request.Sql, "Вьюха сохранена")) return;

        await OpenObjectAsync(request.SchemaName, request.ViewName);
    }

    async Task ShowViewDefinitionAsync()
    {
        if (ViewObject() is not { } table) return;

        var response = await service.ViewDefinition(DataSourceConfigSlug, table.TableSchema.SchemaName, table.TableName);

        var dialog = await _dialogService.ShowDialogAsync<ViewDefinitionDialog>(
            new ViewDefinitionDialogContent(DisplayName(table), response.Sql),
            new DialogParameters
            {
                Title = $"Определение: {DisplayName(table)}",
                Width = "min(900px, 95vw)",
                Modal = true,
                PreventDismissOnOverlayClick = true,
            });

        var result = await dialog.Result;

        if (result.Cancelled || result.Data is not string definition || string.IsNullOrWhiteSpace(definition)) return;

        _viewSource = table;

        if (activeTab is not { } tab) return;

        tab.Sql = definition;
        _editorNeedsSync = true;

        StateHasChanged();
    }

    async Task DropViewAsync()
    {
        if (ViewObject() is not { } table) return;

        var plan = ViewDdlBuilder.Drop(SqlDialectMapping.Dialect(source?.Driver), table.TableSchema.SchemaName, table.TableName);

        if (!plan.Ok)
        {
            _ = _messageService.Error(plan.Error!);
            return;
        }

        var dialog = await _dialogService.ShowDialogAsync<DeleteConfirmationDialog>(
            (MarkupString)$"Удалить вьюху <b>{DisplayName(table)}</b>?<br/><code>{plan.Sql}</code>",
            new DialogParameters
            {
                Title = "Удаление вьюхи",
                Modal = true,
                PreventDismissOnOverlayClick = false,
            });

        if ((await dialog.Result).Cancelled) return;

        await ExecuteViewDdlAsync(plan.Sql!, "Вьюха удалена");
    }

    /// <summary>
    /// Выполнить собранный DDL вьюхи и перечитать структуру. Кэш структуры сбрасывает сервер:
    /// `NonQuery` видит DDL (`CREATE`/`DROP`) и снимает его сам.
    /// </summary>
    async Task<bool> ExecuteViewDdlAsync(string sql, string success)
    {
        var response = await service.NonQuery(DataSourceConfigSlug, new SqlRequest { Sql = sql });

        if (!response.Ok)
        {
            _ = _messageService.Error(response.Message);
            return false;
        }

        _ = _messageService.Success(success);
        _viewSource = null;

        var current = activeTab?.Table;

        await LoadAsync();

        if (activeTab is not { } tab || current is null) return true;

        // После DROP объекта в перечитанной структуре уже нет — вкладка перестаёт быть вьюхой.
        tab.Table = FindObject(current.TableSchema.SchemaName, current.TableName);

        return true;
    }

    async Task OpenObjectAsync(string schemaName, string viewName)
    {
        if (FindObject(schemaName, viewName) is { } table)
        {
            await OpenTableAsync(table);
        }
    }

    QTableResponse? FindObject(string schemaName, string tableName)
        => database?.Tables.FirstOrDefault(t =>
            t.TableName == tableName && t.TableSchema.SchemaName == schemaName);

    /// <summary>Схемы для выбора в диалоге вьюхи — из уже загруженной структуры.</summary>
    List<string> Schemas()
        => database is null
            ? []
            : database.Tables
                .Select(t => t.TableSchema.SchemaName)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

    static string DisplayName(QTableResponse table)
        => string.IsNullOrEmpty(table.TableSchema.SchemaName)
            ? table.TableName
            : $"{table.TableSchema.SchemaName}.{table.TableName}";

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
