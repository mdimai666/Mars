using Mars.Admin.Framework.Components;
using Mars.Admin.Framework.Services;
using Mars.AiChat.Front.Services;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Front.Components;
using Mars.Datasource.Front.Services;
using MarsCodeEditor2;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Datasource.Front.Workspaces;

/// <summary>
/// Общее для страниц запросов: каталог источника, вкладки, редактор, запуск запроса и подтверждения.
/// Мир делится на два: у SQL своя страница, у остальных типов источников — страница объектов.
/// Мост к ИИ-агенту (IAiChatPageHandler) — в partial-файле QueryWorkspaceAiChat.cs.
/// </summary>
public abstract partial class QueryWorkspaceBase : ComponentBase
{
    [Inject] protected IDatasourceServiceClient service { get; set; } = default!;
    [Inject] protected Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;
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

    /// <summary>
    /// Список источников, если его уже загрузила страница-диспетчер: второй раз за ним не ходим.
    /// </summary>
    [Parameter]
    public IReadOnlyCollection<SelectDatasourceDto>? Sources { get; set; }

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

    /// <summary>
    /// Профиль типа источника: язык, подсветка, подсказки и возможности приходят от провайдера
    /// вместе с каталогом, поэтому страница не сравнивает <see cref="DatasourceConfig.Kind"/>.
    /// </summary>
    protected DatasourceKindProfile? profile => catalog?.Profile;

    protected bool Has(string feature) => profile?.Has(feature) == true;

    protected bool CanManageViews => Has(DatasourceFeature.Views);

    protected bool CanWrite => Has(DatasourceFeature.Write);

    /// <summary>Клик по объекту дерева переходит к его тексту в документе, а не выполняет запрос.</summary>
    protected bool OpensAsDocument => profile?.OpensAsDocument == true;

    protected int objectCount => catalog?.Groups.Sum(group => group.Objects.Count) ?? 0;

    /// <summary>Язык запроса по умолчанию для источника: его задаёт провайдер.</summary>
    protected string DefaultLanguage => profile?.DefaultLanguage ?? DatasourceLanguage.Sql;

    /// <summary>Подсветка редактора: идентификатор языка Monaco из профиля провайдера.</summary>
    protected string EditorLang => string.IsNullOrWhiteSpace(profile?.EditorLanguage)
        ? CodeEditor2.Language.sql
        : profile!.EditorLanguage;

    /// <summary>Подсказка под редактором: текст даёт провайдер.</summary>
    protected string Hint => profile?.Hint ?? "";

    /// <summary>Что открыто справа в тулбаре: объект дерева; у миров с операциями — ещё и операция.</summary>
    protected virtual string TrailingLabel
        => activeTab?.Object is { } opened ? DisplayName(new CatalogEntry(activeTab.Schema, opened)) : "";

    protected virtual string? TrailingTitle => null;

    /// <summary>Подпись источника в шапке: тип, а у типа с вариантами — ещё и вариант.</summary>
    protected string SourceLabel => profile is null
        ? ""
        : string.IsNullOrEmpty(profile.Driver) ? profile.KindLabel : $"{profile.KindLabel} · {profile.Label}";

    /// <summary>Сообщение о пустом запросе: текст даёт провайдер.</summary>
    protected virtual string EmptyRequestMessage
        => string.IsNullOrWhiteSpace(profile?.EmptyRequestMessage) ? "Введите запрос" : profile!.EmptyRequestMessage;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        AddTab();
        _ = LoadAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) AiChatPageHandlerHolder.Current = this;

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
            listDatasources = Sources ?? await service.ListSelectDatasource();
            Tree.ApplyDefaults(source?.Slug, catalog);
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
            catalog = await service.Catalog(DataSourceConfigSlug, refresh: true);
            Tree.ApplyDefaults(source?.Slug, catalog);
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
    protected Task RunActiveTabAsync() => RunTabAsync(null);

    /// <summary>Выполнить блок документа по номеру строки: CodeLens «выполнить» над строкой запроса.</summary>
    protected Task RunBlockAtLineAsync(int line) => RunTabAsync(line);

    async Task RunTabAsync(int? atLine)
    {
        var tab = activeTab;
        if (tab is null || tab.Loading) return;

        string text = await ReadEditorTextAsync();

        if (tab.IsDocument)
        {
            // В документе лежат все запросы: выполняем тот, в котором стоит курсор (или на который указал CodeLens)
            var block = DocumentText.BlockAt(text, atLine ?? await CursorLineAsync());

            if (block is null)
            {
                _ = _messageService.Error("Поставьте курсор в текст запроса между разделителями «###»");
                return;
            }

            // Параметры выполняемого блока принадлежат ему: страница перепривязывает форму до отправки.
            BeforeRunDocumentBlock(tab, text, block);

            // На сервер блок уходит отдельным текстом: переменные из шапки документа он не увидит.
            // Первый блок содержит их сам (преамбула — часть блока), остальным подставляем в начало.
            var globals = block.StartLine > 1 ? DocumentText.DocumentVariables(text) : "";

            text = globals.Length > 0 ? globals + "\n" + block.Text : block.Text;
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

    /// <summary>Блок документа, который сейчас будет выполнен: страница может привязать к нему форму параметров.</summary>
    protected virtual void BeforeRunDocumentBlock(QueryTab tab, string text, DocumentBlock block)
    {
    }

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

    /// <summary>
    /// Общий сброс вкладки под открываемый объект: контекст каталога, ключевые колонки,
    /// очистка прежнего результата и правок. Текст запроса задаёт страница мира сама
    /// (Sql собирает browse-SQL, Objects оставляет пустым) и сама запускает запрос.
    /// </summary>
    protected void ResetTabForObject(QueryTab tab, CatalogEntry entry)
    {
        // Открытый объект должен быть виден в дереве, даже если его группу свернули.
        Tree.Expand(entry.Group);

        tab.Object = entry.Object;
        tab.Schema = entry.Group;
        tab.Language = string.IsNullOrWhiteSpace(entry.Object.DefaultLanguage) ? DefaultLanguage : entry.Object.DefaultLanguage;
        tab.SourceWritable = CanWrite;
        tab.KeyColumns = entry.Object.Fields.Where(c => c.IsKey).Select(c => c.Name).ToList();
        tab.Title = entry.Object.Name;
        tab.BrowseLimit = DefaultBrowseLimit;
        tab.Total = null;
        tab.TotalNote = null;
        tab.Changes.Clear();
        tab.ShowJson = false;
        tab.ParameterValues.Clear();
        tab.Operation = null;

        _editorNeedsSync = true;
    }

    protected async Task<bool> ConfirmChangeAsync(string keyword, string what)
    {
        var dialog = await _dialogService.ShowDialogAsync<DeleteConfirmationDialog>(
            (MarkupString)$"Запрос <b>{System.Net.WebUtility.HtmlEncode(keyword)}</b> {what}. Выполнить?",
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
