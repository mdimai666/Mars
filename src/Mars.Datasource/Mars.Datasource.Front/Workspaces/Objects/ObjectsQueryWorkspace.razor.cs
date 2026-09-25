using Mars.Admin.Framework.Extensions;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Front.Services;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Datasource.Front.Workspaces.Objects;

/// <summary>
/// Страница запросов не-SQL источника (файл, REST, дальше — GraphQL и прочие): дерево объектов
/// и операций, редактор языка источника, форма параметров операции и документ запросов `requests.http`.
/// </summary>
public partial class ObjectsQueryWorkspace
{
    /// <summary>
    /// Операция, чьи параметры показывает форма: у документа запросов — та, к которой перешло дерево,
    /// у остальных вкладок — открытый объект.
    /// </summary>
    DatasourceCatalogObject? FormOperation => activeTab?.Operation ?? activeTab?.Object;

    /// <summary>Справа в тулбаре: открытый объект, а если его нет — операция, к которой перешло дерево.</summary>
    protected override string TrailingLabel
        => base.TrailingLabel.Length > 0 ? base.TrailingLabel : activeTab?.Operation?.Id ?? "";

    protected override string? TrailingTitle
        => base.TrailingLabel.Length > 0 ? null : "Операция каталога, к которой перешло дерево";

    /// <summary>Запись в источник требует подтверждения: метод запроса виден в его тексте.</summary>
    protected override async Task<bool> ConfirmRunAsync(QueryTab tab, string text)
        => !CanWrite
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
        // У источника с документом запросов редактор один — весь документ: дерево только переходит
        // к нужному запросу и никогда не выполняет его кликом.
        if (OpensAsDocument)
        {
            await OpenDocumentBlockAsync(entry);
            return;
        }

        var tab = activeTab ?? AddTab();

        ResetTabForObject(tab, entry);

        // У файла текст запроса — условие фильтра, а сам объект уходит в запросе.
        tab.BrowseSql = null;
        tab.Text = "";
        tab.MaxRows = DefaultBrowseLimit;

        await SyncEditorAsync();
        await RunActiveTabAsync();
    }

    //=== документ запросов (rest) =============================================

    readonly string _examplesButtonId = "ds-http-examples-" + Guid.NewGuid().ToString("N");

    bool _examplesOpen;

    /// <summary>Имя документа запросов источника объявляет провайдер (у rest это requests.http).</summary>
    string DocumentName => profile?.DocumentName ?? DatasourceSettings.RequestsDocument;

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
                Title = DocumentName,
                Language = DatasourceLanguage.Http,
                IsDocument = true,
            };

            tabs.Add(tab);

            try
            {
                tab.Text = await service.Document(DataSourceConfigSlug, DocumentName);
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
        var block = entry.Object.Operation?.Line is > 0 and int line
            ? DocumentText.BlockAt(text, line)
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
            var result = await service.SaveDocument(DataSourceConfigSlug, DocumentName, text);

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

        var firstLine = block.Text.Split('\n')[0].Trim();
        var ok = await _dialogService.MarsDeleteConfirmation(
            $"Удалить блок запроса из документа?<br/><code>{System.Net.WebUtility.HtmlEncode(firstLine)}</code>");

        if (!ok) return;

        tab.Text = DocumentText.Remove(text, block);

        await SyncEditorAsync();

        _ = _messageService.Success("Блок удалён из документа — не забудьте сохранить");
    }

    /// <summary>Дописать пример запроса в конец документа и показать его (на сервере — только после сохранения).</summary>
    async Task AddExampleAsync(HttpDocumentExample example)
    {
        if (activeTab is not { IsDocument: true } tab) return;

        tab.Text = DocumentText.Append(await ReadEditorTextAsync(), example.Text);

        await SyncEditorAsync();

        var block = DocumentText.Blocks(tab.Text).LastOrDefault();

        if (block is not null && _editor is not null) await _editor.RevealLinesAsync(block.StartLine, block.EndLine);

        _ = _messageService.Success("Пример добавлен — не забудьте сохранить документ");
    }

    /// <summary>Ctrl+S в редакторе: сохраняется документ запросов, остальные вкладки — черновики.</summary>
    protected override async Task OnEditorSaveAsync(string value)
    {
        if (activeTab is not { IsDocument: true }) return;

        await SaveDocumentAsync();
    }

    //=== примеры условий (file) ================================================

    readonly string _fileExamplesButtonId = "ds-file-examples-" + Guid.NewGuid().ToString("N");

    bool _fileExamplesOpen;

    /// <summary>
    /// Вставить пример условия в редактор. Текст запроса к файлу — одно выражение, поэтому
    /// к непустому условию пример дописывается через «&&», а не заменяет набранное.
    /// </summary>
    async Task AddFileExampleAsync(FileQueryExample example)
    {
        if (activeTab is not { IsDocument: false } tab) return;

        var text = (await ReadEditorTextAsync()).Trim();

        tab.Text = text.Length == 0 ? example.Text : $"{text} && {example.Text}";

        await SyncEditorAsync();

        _ = _messageService.Success("Пример вставлен — поправьте имена колонок под свой файл");
    }

    //=== форма параметров и блок под курсором ==================================

    readonly DocumentBlockBinding _binding = new();

    /// <summary>Курсор перешёл на другую строку: форма следует за блоком под курсором.</summary>
    async Task OnCursorLineAsync(int line)
    {
        if (activeTab is not { IsDocument: true }) return;

        await BindDocumentFormAsync(line, strict: true);
    }

    /// <summary>
    /// Текст документа поправили: перечитать форму из блока под курсором. Нестрого — пока
    /// пользователь печатает адрес, снятие привязки только мешало бы.
    /// </summary>
    async Task OnContentChangedAsync()
    {
        if (activeTab is not { IsDocument: true }) return;

        await BindDocumentFormAsync(await CursorLineAsync(), strict: false);
    }

    async Task BindDocumentFormAsync(int line, bool strict)
    {
        if (activeTab is not { IsDocument: true } tab) return;

        var text = await ReadEditorTextAsync();

        if (!_binding.Bind(text, line, catalog, strict)) return;

        ApplyBinding(tab);

        StateHasChanged();
    }

    /// <summary>Перенести привязку во вкладку: операция формы и значения параметров из текста блока.</summary>
    void ApplyBinding(QueryTab tab)
    {
        if (_binding.Operation is { } operation)
        {
            SetOperation(tab, operation);

            tab.ParameterValues.Clear();

            foreach (var pair in _binding.Values) tab.ParameterValues[pair.Key] = pair.Value;

            return;
        }

        tab.Operation = null;
        tab.ParameterValues.Clear();
    }

    /// <summary>Выполняется конкретный блок: форма и отправляемые параметры перепривязываются на него.</summary>
    protected override void BeforeRunDocumentBlock(QueryTab tab, string text, DocumentBlock block)
    {
        _binding.Bind(text, block.StartLine, catalog, strict: true);

        ApplyBinding(tab);
    }

    /// <summary>
    /// Правка параметра в форме: значение сразу перезаписывается в текст блока под курсором —
    /// текст документа остаётся источником правды, а запрос можно выполнить и без формы.
    /// </summary>
    async Task SetDocumentParameterAsync((string Name, string? Value) change)
    {
        SetParameter(change.Name, change.Value);

        if (activeTab is not { IsDocument: true } tab || tab.Operation is null) return;
        if (_editor is null || !_editorReady) return;

        var text = await ReadEditorTextAsync();

        var block = DocumentText.BlockAt(text, await CursorLineAsync())
            ?? (_binding.Block is { } bound ? DocumentText.BlockAt(text, bound.StartLine) : null);

        if (block is null) return;

        var updated = HttpBlockSync.ApplyValues(block.Text, tab.Operation, tab.ParameterValues);

        if (updated == block.Text) return;

        await _editor.ReplaceLinesAsync(block.StartLine, block.EndLine, updated);
    }
}
