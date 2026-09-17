using Mars.Admin.Framework.Components;
using Mars.Admin.Framework.Services;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Contracts.Models;
using Mars.Datasource.Front.Components;
using Mars.Datasource.Front.Services;
using MarsCodeEditor2;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Datasource.Front.Workspaces;

/// <summary>
/// Общее для страниц запросов: каталог источника, вкладки, редактор, запуск запроса и подтверждения.
/// Мир делится на два: у SQL своя страница, у остальных типов источников — страница объектов.
/// </summary>
public abstract class QueryWorkspaceBase : ComponentBase
{
    [Inject] protected IDatasourceServiceClient service { get; set; } = default!;
    [Inject] protected Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] protected IAIToolAppService _aiTool { get; set; } = default!;
    [Inject] protected IDialogService _dialogService { get; set; } = default!;
    [Inject] protected NavigationManager nav { get; set; } = default!;

    protected bool Busy;
    protected string? errorMessage;

    /// <summary>Состояние дерева: фильтр, свёрнутые группы и их вычисление для разметки.</summary>
    protected WorkspaceTreeState Tree { get; } = new();

    /// <summary>Сколько строк открывать при клике по объекту: смотрим данные, а не выгружаем таблицу.</summary>
    protected const int DefaultBrowseLimit = 50;

    /// <summary>Сколько секунд ждём `COUNT(*)`: отменить запрос из UI пока нельзя, лучше честно сказать «не сосчитали».</summary>
    protected const int TotalCountTimeoutSec = 5;

    protected string _slug = DatasourceConfig.DefaultSlug;

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

    protected DatasourceCatalog? catalog;
    protected IReadOnlyCollection<SelectDatasourceDto> listDatasources = [];

    protected List<QueryTab> tabs = [];
    protected string? activeTabId;

    protected QueryTab? activeTab => tabs.FirstOrDefault(t => t.Id == activeTabId) ?? tabs.FirstOrDefault();

    protected SelectDatasourceDto? source => listDatasources.FirstOrDefault(s => s.Slug == DataSourceConfigSlug);

    protected CodeEditor2? _editor;

    /// <summary>JS-редактор создаётся не в момент появления ссылки на компонент, а в OnInit —
    /// до этого SetValue/GetValue падают с «Couldn't find the editor with id».</summary>
    protected bool _editorReady;

    protected bool _editorNeedsSync;

    /// <summary>Язык, под который создан текущий JS-редактор: при смене язык меняется и компонент (по `@key`).</summary>
    protected string? _editorLang;

    protected string datasourceConfigUrl => $"{nav.BaseUri}datasource/config";

    protected bool IsSql => string.Equals(catalog?.Kind, DatasourceKind.Sql, StringComparison.OrdinalIgnoreCase);

    protected bool IsRest => string.Equals(catalog?.Kind, DatasourceKind.Rest, StringComparison.OrdinalIgnoreCase);

    protected bool CanManageViews => catalog?.Capabilities.CanManageViews == true;

    /// <summary>Объект — http-запрос (операция rest-провайдера или запрос документа).</summary>
    protected static bool IsHttpObject(DatasourceCatalogObject obj)
        => string.Equals(obj.DefaultLanguage, DatasourceLanguage.Http, StringComparison.OrdinalIgnoreCase);

    protected int objectCount => catalog?.Groups.Sum(group => group.Objects.Count) ?? 0;

    /// <summary>Язык запроса по умолчанию для источника: SQL у базы, `.http` у REST, Dynamic LINQ у файла.</summary>
    protected string DefaultLanguage => IsSql ? DatasourceLanguage.Sql : IsRest ? DatasourceLanguage.Http : DatasourceLanguage.Linq;

    /// <summary>
    /// Язык подсветки редактора: SQL у базы, C# у запроса на Dynamic LINQ.
    /// Подсветки `.http` в бандле monaco нет — запрос REST остаётся обычным текстом.
    /// </summary>
    protected string EditorLang => DefaultLanguage switch
    {
        DatasourceLanguage.Linq => CodeEditor2.Language.csharp,
        DatasourceLanguage.Http => CodeEditor2.Language.plaintext,
        _ => CodeEditor2.Language.sql,
    };

    /// <summary>Подпись источника в шапке: тип, а у sql ещё и движок.</summary>
    protected string SourceLabel => catalog is null
        ? ""
        : IsSql && source is not null ? $"{catalog.Kind} · {source.Driver}" : catalog.Kind;

    /// <summary>Сообщение о пустом запросе: у каждого мира своё.</summary>
    protected abstract string EmptyRequestMessage { get; }

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

    protected async Task OnEditorInitAsync()
    {
        _editorReady = true;

        await SyncEditorAsync();
    }

    protected virtual async Task LoadAsync()
    {
        Busy = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            catalog = await service.Catalog(DataSourceConfigSlug);
            listDatasources = await service.ListSelectDatasource();
            Tree.ApplyDefaults(source?.Slug, source?.Driver, catalog);
            ApplySourceLanguage();
        }
        catch (Exception ex)
        {
            // Уже загруженный каталог не выбрасываем: ошибка обновления — не повод гасить рабочую область
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
    protected void ApplySourceLanguage()
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

    protected async Task RefreshCatalogAsync()
    {
        Busy = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            catalog = await service.RefreshCatalog(DataSourceConfigSlug);
            Tree.ApplyDefaults(source?.Slug, source?.Driver, catalog);
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

    protected QueryTab AddTab()
    {
        var tab = new QueryTab { Title = $"query {tabs.Count + 1}", Language = DefaultLanguage };

        tabs.Add(tab);
        activeTabId = tab.Id;
        _editorNeedsSync = true;

        StateHasChanged();

        return tab;
    }

    protected async Task SelectTabAsync(QueryTab tab)
    {
        if (tab == activeTab) return;

        await RememberEditorTextAsync();

        activeTabId = tab.Id;
        _editorNeedsSync = true;

        StateHasChanged();
    }

    protected async Task CloseTabAsync(QueryTab tab)
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
    protected async Task RememberEditorTextAsync()
    {
        var tab = activeTab;
        if (!_editorReady || tab is null || _editor is null) return;

        var text = await _editor.GetValue();

        // У документа пустой текст — тоже правка: очищенный документ должен остаться пустым
        if (tab.IsDocument || !string.IsNullOrWhiteSpace(text)) tab.Text = text;
    }

    protected async Task SyncEditorAsync()
    {
        if (!_editorReady || _editor is null || activeTab is null) return;

        _editorNeedsSync = false;
        await _editor.SetValue(activeTab.Text);
    }

    protected async Task<string> ReadEditorTextAsync()
    {
        if (!_editorReady || _editor is null) return activeTab?.Text ?? "";

        return await _editor.GetValue();
    }

    protected async Task<int> CursorLineAsync()
        => _editor is null ? 1 : await _editor.GetCursorLineAsync();

    /// <summary>Ctrl+S в редакторе: у документа запросов это сохранение, у остальных вкладок — черновик.</summary>
    protected virtual Task OnEditorSaveAsync(string value) => Task.CompletedTask;

    //=== выполнение ===========================================================

    /// <summary>
    /// Выполнить активную вкладку. Порядок общий для обоих миров, различия вынесены в хуки:
    /// что считать пустым запросом, какое подтверждение спросить и что делать с результатом.
    /// </summary>
    protected async Task RunActiveTabAsync()
    {
        var tab = activeTab;
        if (tab is null || tab.Loading) return;

        string text = await ReadEditorTextAsync();

        if (tab.IsDocument)
        {
            // В документе лежат все запросы: выполняем тот, в котором стоит курсор
            var block = DocumentText.BlockAt(text, await CursorLineAsync());

            if (block is null)
            {
                _ = _messageService.Error("Поставьте курсор в текст запроса между разделителями «###»");
                return;
            }

            text = block.Text;
        }

        if (string.IsNullOrWhiteSpace(text) && !AllowsEmptyQuery(tab))
        {
            _ = _messageService.Error(EmptyRequestMessage);
            return;
        }

        if (!await ConfirmRunAsync(tab, text)) return;

        tab.Text = text;
        tab.Title = tab.Object?.Name ?? tab.Title;
        tab.Loading = true;
        tab.Error = null;

        StateHasChanged();

        try
        {
            var result = await service.Query(DataSourceConfigSlug, new DatasourceRequest
            {
                // Текст из документа самодостаточен: ObjectId подсказал бы серверу другой запрос документа
                ObjectId = tab.IsDocument ? null : tab.Object?.Id,
                Language = tab.Language,
                Query = text,
                Parameters = ParameterList(tab),
                MaxRows = tab.MaxRows,
            });

            tab.Result = result;
            tab.Error = result.Ok ? null : result.Message;

            AfterResult(tab, text, result);
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

    /// <summary>Можно ли выполнять пустой текст: у файла это «все строки объекта», у SQL выполнять нечего.</summary>
    protected virtual bool AllowsEmptyQuery(QueryTab tab) => tab.Object is not null;

    /// <summary>Подтверждение перед выполнением (опасный SQL, запись у REST).</summary>
    protected virtual Task<bool> ConfirmRunAsync(QueryTab tab, string text) => Task.FromResult(true);

    /// <summary>Что делать с готовым результатом (счёт строк у SQL, документ у REST).</summary>
    protected virtual void AfterResult(QueryTab tab, string text, QueryResultDto result)
    {
    }

    /// <summary>
    /// Просим больше строк. У просмотра sql-объекта лимит стоит в самом SQL — там страница растит его
    /// и пересобирает запрос; у остальных случаев ограничение серверное.
    /// </summary>
    protected virtual async Task RunMoreAsync()
    {
        if (activeTab is { } tab) tab.MaxRows *= 5;

        await RunActiveTabAsync();
    }

    /// <summary>Открыть объект каталога: у каждого мира свой смысл клика по дереву.</summary>
    protected abstract Task OpenObjectAsync(CatalogEntry entry);

    protected async Task<bool> ConfirmChangeAsync(string keyword, string what)
    {
        var dialog = await _dialogService.ShowDialogAsync<DeleteConfirmationDialog>(
            (MarkupString)$"Запрос <b>{keyword}</b> {what}. Выполнить?",
            new DialogParameters
            {
                Title = "Подтверждение запроса",
                Modal = true,
                PreventDismissOnOverlayClick = false,
            });

        var result = await dialog.Result;

        return !result.Cancelled;
    }

    //=== каталог: поиск и подписи =============================================

    protected CatalogEntry? FindObject(string schemaName, string objectName)
        => catalog?.Groups
            .Where(group => string.Equals(group.Name, schemaName, StringComparison.Ordinal))
            .SelectMany(group => group.Objects.Select(obj => new CatalogEntry(group.Name, obj)))
            .FirstOrDefault(entry => string.Equals(entry.Object.Name, objectName, StringComparison.Ordinal));

    /// <summary>Группы для выбора в диалоге вьюхи — из уже загруженного каталога.</summary>
    protected List<string> Schemas()
        => catalog is null
            ? []
            : catalog.Groups
                .Select(group => group.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

    protected static string DisplayName(CatalogEntry entry)
        => string.IsNullOrEmpty(entry.Group) ? entry.Object.Name : $"{entry.Group}.{entry.Object.Name}";

    //=== параметры операции ====================================================

    /// <summary>Заполненные значения параметров вкладки; пустые не отправляем — у источника есть свои defaults.</summary>
    protected static List<DatasourceParam>? ParameterList(QueryTab tab)
    {
        var values = tab.ParameterValues
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => new DatasourceParam { Name = pair.Key, Value = pair.Value })
            .ToList();

        return values.Count == 0 ? null : values;
    }

    protected string? ParameterValue(string name)
        => activeTab is { } tab && tab.ParameterValues.TryGetValue(name, out var value) ? value : null;

    protected void SetParameter(string name, string? value)
    {
        if (activeTab is not { } tab) return;

        if (string.IsNullOrWhiteSpace(value)) tab.ParameterValues.Remove(name);
        else tab.ParameterValues[name] = value;
    }
}
