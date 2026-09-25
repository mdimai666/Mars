using Mars.Admin.Framework.Components;
using Mars.Admin.Framework.Extensions;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Front.Components;
using Mars.Datasource.Front.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Datasource.Front.Workspaces.Sql;

/// <summary>
/// Страница запросов SQL-источника: своё дерево (схемы, таблицы, вьюхи), просмотр объекта
/// с лимитом строк в самом SQL, счёт «всего N», управление вьюхами и подтверждение опасных запросов.
/// </summary>
public partial class SqlQueryWorkspace
{
    /// <summary>Пустой SQL выполнять нечего (в отличие от файла, где пустое условие — «все строки»).</summary>
    protected override bool AllowsEmptyQuery(QueryTab tab) => false;

    protected override async Task<bool> ConfirmRunAsync(QueryTab tab, string text)
        => !SqlSafety.IsDestructive(text)
           || await ConfirmChangeAsync(SqlSafety.FirstWord(text), "изменяет данные или структуру базы");

    /// <summary>
    /// «Всего N» показываем только для нашего просмотра объекта: у произвольного запроса непонятно,
    /// что считать. Меньше лимита строк — количество известно и так; ровно лимит — считаем в фоне.
    /// </summary>
    protected override void AfterResult(QueryTab tab, string text, QueryResultDto result)
        => StartTotalCount(tab, text, result);

    /// <summary>
    /// Просим больше строк: у просмотра объекта лимит стоит в самом SQL — растим его и пересобираем запрос.
    /// </summary>
    protected override async Task RunMoreAsync()
    {
        var tab = activeTab;
        if (tab is null) return;

        if (tab.IsBrowse)
        {
            tab.BrowseLimit *= 5;
            tab.BrowseSql = BuildBrowseSql(new CatalogEntry(tab.Schema, tab.Object!), tab.BrowseLimit);
            tab.Text = tab.BrowseSql;

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

    protected override async Task OpenObjectAsync(CatalogEntry entry)
    {
        var tab = activeTab ?? AddTab();

        // Открыли другой объект — «изменяем вьюху» больше не про него.
        _viewSource = null;

        ResetTabForObject(tab, entry);

        tab.BrowseSql = BuildBrowseSql(entry, tab.BrowseLimit);
        tab.Text = tab.BrowseSql;
        EnsureBrowseCap(tab);

        await SyncEditorAsync();
        await RunActiveTabAsync();
    }

    //=== просмотр объекта =====================================================

    /// <summary>
    /// Просмотр объекта: лимит строк ставится в сам SQL, поэтому серверный предел должен быть выше —
    /// иначе «Показать больше» за серверный предел ничего не покажет.
    /// </summary>
    void EnsureBrowseCap(QueryTab tab)
        => tab.MaxRows = Math.Max(tab.MaxRows, tab.BrowseLimit + 1);

    string BuildBrowseSql(CatalogEntry entry, int limit)
        => BrowseSqlBuilder.Build(
            SqlDialectMapping.Dialect(source?.Driver),
            entry.Group,
            entry.Object.Name,
            entry.Object.Fields
                .Where(c => c.IsKey)
                .OrderBy(c => c.Ordinal)
                .Select(c => c.Name)
                .ToList(),
            limit);

    string BuildCountSql(QueryTab tab)
        => BrowseSqlBuilder.Count(SqlDialectMapping.Dialect(source?.Driver), tab.Schema, tab.Object!.Name);

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
            var count = await service.Query(DataSourceConfigSlug, new DatasourceRequest
            {
                Query = BuildCountSql(tab),
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

    //=== вьюхи ================================================================

    /// <summary>
    /// Вьюха, определение которой загружено в редактор: диалог вьюхи предзаполняется ею
    /// (сценарий «изменить существующую»).
    /// </summary>
    CatalogEntry? _viewSource;

    /// <summary>
    /// Активный объект, если это обычная вьюха. Матвьюхи — вне этой фазы: у них другой DDL,
    /// и кнопки, которые на них падают, показывать не стоит.
    /// </summary>
    CatalogEntry? ViewObject()
        => activeTab is { Object: { ObjectType: DatasourceObjectType.View } } tab
            ? new CatalogEntry(tab.Schema, tab.Object)
            : null;

    async Task CreateViewAsync()
    {
        var content = new CreateViewDialogContent(
            SqlDialectMapping.Dialect(source?.Driver),
            Schemas(),
            await ReadEditorTextAsync(),
            _viewSource?.Group ?? activeTab?.Schema,
            _viewSource?.Object.Name,
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
        if (ViewObject() is not { } entry) return;

        ViewDefinitionResponse response;

        try
        {
            response = await service.ViewDefinition(DataSourceConfigSlug, entry.Group, entry.Object.Name);
        }
        catch (Exception ex)
        {
            // Исключение из обработчика события роняет рабочую область целиком — сообщаем словами
            _ = _messageService.Error(ex.Message);
            return;
        }

        var dialog = await _dialogService.ShowDialogAsync<ViewDefinitionDialog>(
            new ViewDefinitionDialogContent(DisplayName(entry), response.Sql),
            new DialogParameters
            {
                Title = $"Определение: {DisplayName(entry)}",
                Width = "min(900px, 95vw)",
                Modal = true,
                PreventDismissOnOverlayClick = true,
            });

        var result = await dialog.Result;

        if (result.Cancelled || result.Data is not string definition || string.IsNullOrWhiteSpace(definition)) return;

        _viewSource = entry;

        if (activeTab is not { } tab) return;

        tab.Text = definition;
        _editorNeedsSync = true;

        StateHasChanged();
    }

    async Task DropViewAsync()
    {
        if (ViewObject() is not { } entry) return;

        var plan = ViewDdlBuilder.Drop(SqlDialectMapping.Dialect(source?.Driver), entry.Group, entry.Object.Name);

        if (!plan.Ok)
        {
            _ = _messageService.Error(plan.Error!);
            return;
        }

        var ok = await _dialogService.MarsDeleteConfirmation(
            $"Удалить вьюху <b>{DisplayName(entry)}</b>?<br/><code>{plan.Sql}</code>");

        if (!ok) return;

        await ExecuteViewDdlAsync(plan.Sql!, "Вьюха удалена");
    }

    /// <summary>
    /// Выполнить собранный DDL вьюхи и перечитать каталог. Кэш сбрасывает сервер:
    /// `NonQuery` видит DDL (`CREATE`/`DROP`) и снимает его сам.
    /// </summary>
    async Task<bool> ExecuteViewDdlAsync(string sql, string success)
    {
        DatasourceModifyResult response;

        try
        {
            response = await service.Modify(DataSourceConfigSlug, new DatasourceRequest { Language = DatasourceLanguage.Sql, Query = sql });
        }
        catch (Exception ex)
        {
            _ = _messageService.Error(ex.Message);
            return false;
        }

        if (!response.Ok)
        {
            _ = _messageService.Error(response.Message);
            return false;
        }

        _ = _messageService.Success(success);
        _viewSource = null;

        var current = activeTab?.Object is { } opened ? new CatalogEntry(activeTab!.Schema, opened) : null;

        await LoadAsync();

        if (activeTab is not { } tab || current is null) return true;

        // После DROP объекта в перечитанном каталоге его уже нет — вкладка перестаёт быть вьюхой.
        tab.Object = FindObject(current.Group, current.Object.Name)?.Object;

        return true;
    }

    async Task OpenObjectAsync(string schemaName, string objectName)
    {
        if (FindObject(schemaName, objectName) is { } entry)
        {
            await OpenObjectAsync(entry);
        }
    }
}
