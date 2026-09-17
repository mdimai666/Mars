using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Contracts.Models;
using Mars.Datasource.Front.Services;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Datasource.Front.Workspaces.Objects;

/// <summary>
/// Страница запросов не-SQL источника (файл, REST, дальше — GraphQL и прочие): дерево объектов
/// и операций, редактор языка источника, форма параметров операции и документ запросов `requests.http`.
/// </summary>
public partial class ObjectsQueryWorkspace
{
    protected override string EmptyRequestMessage => IsRest
        ? "Напишите HTTP-запрос или выберите операцию слева"
        : "Выберите объект слева или напишите условие";

    /// <summary>
    /// Операция, чьи параметры показывает форма: у документа запросов — та, к которой перешло дерево,
    /// у остальных вкладок — открытый объект.
    /// </summary>
    DatasourceCatalogObject? FormOperation => activeTab?.Operation ?? activeTab?.Object;

    protected override async Task<bool> ConfirmRunAsync(QueryTab tab, string text)
        => !IsRest
           || !RestSafety.IsWrite(RestSafety.FirstMethod(text))
           || await ConfirmChangeAsync(RestSafety.FirstMethod(text), "меняет данные источника");

    /// <summary>
    /// Не-sql источник сообщает общее число записей сам (WordPress — заголовком `X-WP-Total`),
    /// а ответ документом в таблицу не раскладывается — показываем его сразу.
    /// </summary>
    protected override void AfterResult(QueryTab tab, string text, QueryResultDto result)
    {
        tab.Total = result.Total;
        tab.TotalNote = null;

        if (result.Rows.Length == 0 && result.Json is not null) tab.ShowJson = true;
    }

    protected override async Task OpenObjectAsync(CatalogEntry entry)
    {
        // У rest-источника редактор один — весь документ запросов: дерево только переходит к нужному
        // запросу. Операция, приехавшая от rest-провайдера, ведёт себя так же, даже если тип источника
        // определён неверно: http-запрос никогда не выполняется кликом, только по кнопке.
        if (IsRest || IsHttpObject(entry.Object))
        {
            await OpenDocumentBlockAsync(entry);
            return;
        }

        var tab = activeTab ?? AddTab();

        // Открытый объект должен быть виден в дереве, даже если его группу свернули.
        Tree.Expand(entry.Group);

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
        tab.ParameterValues.Clear();
        tab.Operation = null;

        // У файла текст запроса — условие фильтра, а сам объект уходит в запросе.
        tab.BrowseSql = null;
        tab.Text = "";
        tab.MaxRows = DefaultBrowseLimit;

        _editorNeedsSync = true;

        await SyncEditorAsync();
        await RunActiveTabAsync();
    }

    //=== документ запросов (rest) =============================================

    /// <summary>
    /// Вкладка документа: одна на источник. Текст берём с сервера только при её открытии —
    /// дальше это текст редактора, иначе несохранённые правки терялись бы при каждом клике по дереву.
    /// </summary>
    async Task<QueryTab?> OpenDocumentAsync()
    {
        var tab = tabs.FirstOrDefault(item => item.IsDocument);

        if (tab is null)
        {
            tab = new QueryTab
            {
                Title = DatasourceSettings.RequestsDocument,
                Language = DatasourceLanguage.Http,
                IsDocument = true,
            };

            tabs.Add(tab);

            try
            {
                tab.Text = await service.Requests(DataSourceConfigSlug);
            }
            catch (Exception ex)
            {
                tab.Error = ex.Message;
            }

            await RememberEditorTextAsync();

            activeTabId = tab.Id;
            _editorNeedsSync = true;

            StateHasChanged();

            return tab;
        }

        if (tab != activeTab) await SelectTabAsync(tab);

        return tab;
    }

    /// <summary>
    /// Переход к запросу в документе: есть в документе — показываем его блок, нет (операция из описания)
    /// — дописываем заготовку в конец и показываем её.
    /// </summary>
    async Task OpenDocumentBlockAsync(CatalogEntry entry)
    {
        var tab = await OpenDocumentAsync();
        if (tab is null) return;

        var text = await ReadEditorTextAsync();
        var block = entry.Object.Line > 0
            ? DocumentText.BlockAt(text, entry.Object.Line)
            : DocumentText.BlockContaining(text, entry.Object.DefaultQuery ?? entry.Object.Id);

        if (block is null && entry.Object.DefaultQuery is { Length: > 0 } draft)
        {
            text = DocumentText.Append(text, draft);
            tab.Text = text;

            await SyncEditorAsync();

            block = DocumentText.Blocks(text).LastOrDefault();
        }

        if (block is null)
        {
            // Запроса нет в документе и заготовки для него тоже — молчать нельзя (документ со сломанным запросом)
            _ = _messageService.Error($"{entry.Object.Name}: запроса нет в документе {DatasourceSettings.RequestsDocument}");
            return;
        }

        // Документ — не объект каталога: показываем, к какому запросу перешли, и сбрасываем прежний результат
        tab.Object = null;
        tab.Schema = "";
        tab.KeyColumns = [];
        tab.SourceWritable = false;
        tab.BrowseSql = null;
        tab.Title = $"{DatasourceSettings.RequestsDocument}: {entry.Object.Name}";
        SetOperation(tab, entry.Object);
        tab.Reset();

        StateHasChanged();

        if (_editor is not null) await _editor.RevealLinesAsync(block.StartLine, block.EndLine);
    }

    /// <summary>
    /// Операция, к которой перешло дерево: её параметры показывает форма. У другой операции
    /// значения чужие, поэтому форму начинаем с чистого листа.
    /// </summary>
    static void SetOperation(QueryTab tab, DatasourceCatalogObject operation)
    {
        if (string.Equals(tab.Operation?.Id, operation.Id, StringComparison.Ordinal)) return;

        tab.Operation = operation;
        tab.ParameterValues.Clear();
    }

    /// <summary>Сохранить документ запросов и перечитать дерево: в нём появятся изменённые запросы.</summary>
    async Task SaveDocumentAsync()
    {
        if (activeTab is not { IsDocument: true } tab) return;

        var text = await ReadEditorTextAsync();

        try
        {
            var result = await service.SaveRequests(DataSourceConfigSlug, text);

            if (result.Ok) _ = _messageService.Success(result.Message);
            else _ = _messageService.Error(result.Message);
        }
        catch (Exception ex)
        {
            _ = _messageService.Error(ex.Message);
            return;
        }

        tab.Text = text;
        tab.Error = null;

        await RefreshCatalogAsync();
    }

    /// <summary>Дублировать блок под курсором: копия встаёт сразу за оригиналом и получает новое имя.</summary>
    async Task DuplicateDocumentBlockAsync()
    {
        if (activeTab is not { IsDocument: true } tab) return;

        var text = await ReadEditorTextAsync();
        var block = DocumentText.BlockAt(text, await CursorLineAsync());

        if (block is null)
        {
            _ = _messageService.Error("Поставьте курсор в блок запроса (между разделителями «###»)");
            return;
        }

        var updated = DocumentText.Duplicate(text, block);

        tab.Text = updated;

        await SyncEditorAsync();

        var copy = DocumentText.Blocks(updated).FirstOrDefault(item => item.StartLine > block.StartLine);

        if (copy is not null && _editor is not null) await _editor.RevealLinesAsync(copy.StartLine, copy.EndLine);

        _ = _messageService.Success("Копия добавлена — не забудьте сохранить документ");
    }

    /// <summary>Убрать блок под курсором из документа (на сервере — только после сохранения).</summary>
    async Task RemoveDocumentBlockAsync()
    {
        if (activeTab is not { IsDocument: true } tab) return;

        var text = await ReadEditorTextAsync();
        var block = DocumentText.BlockAt(text, await CursorLineAsync());

        if (block is null)
        {
            _ = _messageService.Error("Поставьте курсор в блок запроса (между разделителями «###»)");
            return;
        }

        tab.Text = DocumentText.Remove(text, block);

        await SyncEditorAsync();

        _ = _messageService.Success("Блок удалён из документа — не забудьте сохранить");
    }

    /// <summary>Ctrl+S в редакторе: сохраняется документ запросов, остальные вкладки — черновики.</summary>
    protected override async Task OnEditorSaveAsync(string value)
    {
        if (activeTab is not { IsDocument: true }) return;

        await SaveDocumentAsync();
    }
}
