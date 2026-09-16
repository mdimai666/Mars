using Mars.Admin.Framework.Components;
using Mars.Admin.Framework.Services;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Contracts.Dto;
using Mars.Datasource.Front.Components;
using Mars.Datasource.Front.Services;
using Mars.Datasource.Contracts.Models;
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

    DatasourceCatalog? catalog;
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

    /// <summary>Язык, под который создан текущий JS-редактор: при смене язык меняется и компонент (по `@key`).</summary>
    string? _editorLang;

    string datasourceConfigUrl => $"{nav.BaseUri}datasource/config";

    bool IsSql => string.Equals(catalog?.Kind, DatasourceKind.Sql, StringComparison.OrdinalIgnoreCase);

    bool CanManageViews => catalog?.Capabilities.CanManageViews == true;

    int objectCount => catalog?.Groups.Sum(group => group.Objects.Count) ?? 0;

    /// <summary>Язык запроса по умолчанию для источника: у sql это SQL, у остальных пока Dynamic LINQ.</summary>
    string DefaultLanguage => IsSql ? DatasourceLanguage.Sql : DatasourceLanguage.Linq;

    /// <summary>Язык подсветки редактора: SQL у базы, C# у запроса на Dynamic LINQ.</summary>
    string EditorLang => DefaultLanguage == DatasourceLanguage.Linq ? CodeEditor2.Language.csharp : CodeEditor2.Language.sql;

    /// <summary>Подпись источника в шапке: тип, а у sql ещё и движок.</summary>
    string SourceLabel => catalog is null
        ? ""
        : IsSql && source is not null ? $"{catalog.Kind} · {source.Driver}" : catalog.Kind;

    /// <summary>Объект каталога вместе с его группой: у sql группа — схема, она нужна для SQL и DDL вьюх.</summary>
    record CatalogEntry(string Group, DatasourceCatalogObject Object);

    IEnumerable<DatasourceCatalogGroup> filteredGroups
    {
        get
        {
            if (catalog is null) yield break;

            foreach (var group in catalog.Groups.OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase))
            {
                var objects = Filtered(group);

                if (objects.Count == 0) continue;

                yield return new DatasourceCatalogGroup { Name = group.Name, Objects = objects };
            }
        }
    }

    List<DatasourceCatalogObject> Filtered(DatasourceCatalogGroup group)
        => string.IsNullOrWhiteSpace(tableFilter)
            ? group.Objects
            : group.Objects.Where(o => o.Name.Contains(tableFilter, StringComparison.OrdinalIgnoreCase)).ToList();

    bool hasMultipleGroups => catalog is not null
        && catalog.Groups.Count(group => group.Objects.Count > 0) > 1;

    /// <summary>Группы, свёрнутые пользователем: в дереве свёрнуто то, что перечислено здесь.</summary>
    readonly HashSet<string> _collapsedSchemas = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Источник, для которого уже разложили группы по умолчанию.</summary>
    string? _schemaDefaultsFor;

    /// <summary>
    /// Раскрыта ли группа. При активном фильтре дерево раскрыто целиком: иначе найденный объект
    /// остался бы спрятанным в свёрнутой группе.
    /// </summary>
    bool IsSchemaExpanded(string schemaName)
        => !string.IsNullOrWhiteSpace(tableFilter) || !_collapsedSchemas.Contains(schemaName);

    void ToggleSchema(string schemaName)
    {
        if (!_collapsedSchemas.Remove(schemaName)) _collapsedSchemas.Add(schemaName);
    }

    /// <summary>
    /// Группа по умолчанию (`public`, у MsSQL — `dbo`) раскрыта, остальные свёрнуты. Раскладываем так
    /// один раз на источник: дальше состояние принадлежит пользователю, иначе свёрнутая вручную группа
    /// разворачивалась бы после каждого обновления. Если группы по умолчанию нет (в MySQL схема — это
    /// сама база, у файла группа одна и без имени), дерево остаётся раскрытым.
    /// </summary>
    void ApplySchemaDefaults(DatasourceCatalog structure)
    {
        if (_schemaDefaultsFor == source?.Slug) return;

        _schemaDefaultsFor = source?.Slug;
        _collapsedSchemas.Clear();

        var defaultSchema = SqlDialectMapping.Dialect(source?.Driver) == SqlDialect.MsSql ? "dbo" : "public";
        var schemas = structure.Groups
            .Select(group => group.Name)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();

        if (!schemas.Contains(defaultSchema, StringComparer.OrdinalIgnoreCase)) return;

        foreach (var schema in schemas)
        {
            if (!string.Equals(schema, defaultSchema, StringComparison.OrdinalIgnoreCase))
            {
                _collapsedSchemas.Add(schema);
            }
        }
    }

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
            catalog = await service.Catalog(DataSourceConfigSlug);
            listDatasources = await service.ListSelectDatasource();
            ApplySchemaDefaults(catalog);
            ApplySourceLanguage();
        }
        catch (Exception ex)
        {
            catalog = null;
            errorMessage = ex.Message;
        }
        finally
        {
            Busy = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Язык запроса принадлежит источнику: вкладки без открытого объекта получают язык источника,
    /// а при смене языка редактор пересоздаётся — Monaco задаёт язык модели при создании.
    /// </summary>
    void ApplySourceLanguage()
    {
        foreach (var tab in tabs)
        {
            if (tab.Object is null) tab.Language = DefaultLanguage;
        }

        if (_editorLang is not null && _editorLang != EditorLang)
        {
            _editorReady = false;
            _editorNeedsSync = true;
        }

        _editorLang = EditorLang;
    }

    async Task RefreshCatalogAsync()
    {
        Busy = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            catalog = await service.RefreshCatalog(DataSourceConfigSlug);
            ApplySchemaDefaults(catalog);
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
        var tab = new QueryTab { Title = $"query {tabs.Count + 1}", Language = DefaultLanguage };

        tabs.Add(tab);
        activeTabId = tab.Id;
        _editorNeedsSync = true;

        StateHasChanged();

        return tab;
    }

    async Task SelectTabAsync(QueryTab tab)
    {
        if (tab == activeTab) return;

        await RememberEditorTextAsync();

        activeTabId = tab.Id;
        _editorNeedsSync = true;

        StateHasChanged();
    }

    async Task CloseTabAsync(QueryTab tab)
    {
        await RememberEditorTextAsync();

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
    async Task RememberEditorTextAsync()
    {
        var tab = activeTab;
        if (!_editorReady || tab is null || _editor is null) return;

        var text = await _editor.GetValue();
        if (!string.IsNullOrWhiteSpace(text)) tab.Text = text;
    }

    async Task SyncEditorAsync()
    {
        if (!_editorReady || _editor is null || activeTab is null) return;

        _editorNeedsSync = false;
        await _editor.SetValue(activeTab.Text);
    }

    async Task<string> ReadEditorTextAsync()
    {
        if (!_editorReady || _editor is null) return activeTab?.Text ?? "";

        return await _editor.GetValue();
    }

    //=== выполнение ===========================================================

    async Task RunActiveTabAsync()
    {
        var tab = activeTab;
        if (tab is null || tab.Loading) return;

        string text = await ReadEditorTextAsync();

        if (string.IsNullOrWhiteSpace(text))
        {
            // У sql пустой запрос выполнять нечего; у файла пустое условие означает «все строки объекта».
            if (IsSql || tab.Object is null)
            {
                _ = _messageService.Error(IsSql ? "SQL-запрос пуст" : "Выберите объект слева или напишите условие");
                return;
            }
        }

        if (IsSql && SqlSafety.IsDestructive(text) && !await ConfirmDestructiveAsync(text)) return;

        tab.Text = text;
        tab.Title = tab.Object?.Name ?? tab.Title;
        tab.Loading = true;
        tab.Error = null;

        StateHasChanged();

        try
        {
            var result = await service.Query(DataSourceConfigSlug, new DatasourceRequest
            {
                ObjectId = tab.Object?.Id,
                Language = tab.Language,
                Query = text,
                MaxRows = tab.MaxRows,
            });

            tab.Result = result;
            tab.Error = result.Ok ? null : result.Message;

            if (IsSql)
            {
                StartTotalCount(tab, text, result);
            }
            else
            {
                tab.Total = null;
                tab.TotalNote = null;
            }
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
    /// Просим больше строк. У просмотра sql-объекта лимит стоит в самом SQL — растим его и пересобираем
    /// запрос; у остальных случаев ограничение серверное, поэтому растём его.
    /// </summary>
    async Task RunMoreAsync()
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

    /// <summary>
    /// «Всего N» показываем только для нашего просмотра sql-объекта: у произвольного запроса непонятно,
    /// что считать, а у не-sql источника `COUNT` нечем взять. Меньше лимита строк — количество известно
    /// и так; ровно лимит — считаем в фоне, чтобы не задерживать показ строк.
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

    async Task OpenObjectAsync(CatalogEntry entry)
    {
        var tab = activeTab ?? AddTab();

        // Открыли другой объект — «изменяем вьюху» больше не про него.
        _viewSource = null;

        // Открытый объект должен быть виден в дереве, даже если его группу свернули.
        _collapsedSchemas.Remove(entry.Group);

        tab.Object = entry.Object;
        tab.Schema = entry.Group;
        tab.Language = string.IsNullOrWhiteSpace(entry.Object.DefaultLanguage) ? DefaultLanguage : entry.Object.DefaultLanguage;
        tab.SourceWritable = catalog?.Capabilities.CanWrite == true;
        tab.KeyColumns = entry.Object.Columns.Where(c => c.IsKey).Select(c => c.Name).ToList();
        tab.Title = entry.Object.Name;
        tab.BrowseLimit = DefaultBrowseLimit;
        tab.Total = null;
        tab.TotalNote = null;
        tab.Changes.Clear();
        tab.ShowJson = false;

        if (IsSql)
        {
            tab.BrowseSql = BuildBrowseSql(entry, tab.BrowseLimit);
            tab.Text = tab.BrowseSql;
            EnsureBrowseCap(tab);
        }
        else
        {
            // У не-sql источника текст запроса — условие фильтра, а сам объект уходит в запросе.
            tab.BrowseSql = null;
            tab.Text = "";
            tab.MaxRows = DefaultBrowseLimit;
        }

        _editorNeedsSync = true;

        await SyncEditorAsync();
        await RunActiveTabAsync();
    }

    /// <summary>
    /// Просмотр sql-объекта: лимит строк ставится в сам SQL, поэтому серверный предел должен быть выше —
    /// иначе «Показать больше» за серверный предел ничего не покажет.
    /// </summary>
    void EnsureBrowseCap(QueryTab tab)
        => tab.MaxRows = Math.Max(tab.MaxRows, tab.BrowseLimit + 1);

    string BuildBrowseSql(CatalogEntry entry, int limit)
        => BrowseSqlBuilder.Build(
            SqlDialectMapping.Dialect(source?.Driver),
            entry.Group,
            entry.Object.Name,
            entry.Object.Columns
                .Where(c => c.IsKey)
                .OrderBy(c => c.Ordinal)
                .Select(c => c.Name)
                .ToList(),
            limit);

    string BuildCountSql(QueryTab tab)
        => BrowseSqlBuilder.Count(SqlDialectMapping.Dialect(source?.Driver), tab.Schema, tab.Object!.Name);

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

        var response = await service.ViewDefinition(DataSourceConfigSlug, entry.Group, entry.Object.Name);

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

        var dialog = await _dialogService.ShowDialogAsync<DeleteConfirmationDialog>(
            (MarkupString)$"Удалить вьюху <b>{DisplayName(entry)}</b>?<br/><code>{plan.Sql}</code>",
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
    /// Выполнить собранный DDL вьюхи и перечитать каталог. Кэш сбрасывает сервер:
    /// `NonQuery` видит DDL (`CREATE`/`DROP`) и снимает его сам.
    /// </summary>
    async Task<bool> ExecuteViewDdlAsync(string sql, string success)
    {
        var response = await service.NonQuery(DataSourceConfigSlug, new DatasourceRequest { Query = sql });

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

    CatalogEntry? FindObject(string schemaName, string objectName)
        => catalog?.Groups
            .Where(group => string.Equals(group.Name, schemaName, StringComparison.Ordinal))
            .SelectMany(group => group.Objects.Select(obj => new CatalogEntry(group.Name, obj)))
            .FirstOrDefault(entry => string.Equals(entry.Object.Name, objectName, StringComparison.Ordinal));

    /// <summary>Группы для выбора в диалоге вьюхи — из уже загруженного каталога.</summary>
    List<string> Schemas()
        => catalog is null
            ? []
            : catalog.Groups
                .Select(group => group.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

    static string DisplayName(CatalogEntry entry)
        => string.IsNullOrEmpty(entry.Group) ? entry.Object.Name : $"{entry.Group}.{entry.Object.Name}";

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
