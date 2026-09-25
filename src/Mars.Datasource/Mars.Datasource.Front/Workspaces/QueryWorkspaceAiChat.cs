using System.Text.Json;
using Mars.AiChat.Front.Services;

namespace Mars.Datasource.Front.Workspaces;

/// <summary>
/// Мост к ИИ-агенту на открытой странице запросов: контекст (источник, вкладки, результат),
/// чтение и запись текста редактора, выполнение активного запроса. Агент работает с этим через
/// инструменты открытой страницы (get_open_page_info / get_open_page_fields / set_open_page_field /
/// save_open_page); регистрация в <see cref="AiChatPageHandlerHolder"/> — в OnAfterRenderAsync,
/// снятие — в Dispose (паттерн EditPostView).
/// </summary>
public abstract partial class QueryWorkspaceBase : IAiChatPageHandler, IDisposable
{
    private static readonly JsonSerializerOptions AiJsonOptions = new() { WriteIndented = false };

    public void Dispose()
    {
        if (ReferenceEquals(AiChatPageHandlerHolder.Current, this))
            AiChatPageHandlerHolder.Current = null;
    }

    public string GetInfo()
    {
        var tab = activeTab;

        return JsonSerializer.Serialize(new
        {
            page = "datasource/query",
            slug = DataSourceConfigSlug,
            source = source?.Title,
            kind = source?.Kind ?? profile?.Kind,
            language = tab?.Language,
            activeTab = tab is null ? null : new
            {
                title = tab.Title,
                obj = tab.Object?.Id,
                document = tab.IsDocument,
                rows = tab.Result?.Rows.Length,
                total = tab.Result?.Total ?? tab.Total,
                error = tab.Error,
            },
            tabs = tabs.Select(item => new { title = item.Title, obj = item.Object?.Id, document = item.IsDocument }),
            fields = new[] { "editor" },
            hint = "editor — текст запроса активной вкладки (SQL, предикат Dynamic LINQ или блок .http). "
                   + "Запись текста не выполняет запрос: для запуска используй save_open_page "
                   + "или предложи пользователю нажать «выполнить».",
        }, AiJsonOptions);
    }

    public async Task<string> GetFields()
        => JsonSerializer.Serialize(new { editor = await ReadEditorTextAsync() }, AiJsonOptions);

    public async Task<string> SetField(string field, string value)
    {
        if (!string.Equals(field, "editor", StringComparison.OrdinalIgnoreCase))
            return $"Неизвестное поле '{field}'. На этой странице доступно только поле editor — текст запроса активной вкладки.";

        var tab = activeTab;
        if (tab is null) return "На странице запросов нет активной вкладки.";

        tab.Text = value ?? "";

        // До готовности JS-редактора SetValue падает: просто помечаем вкладку к синхронизации
        if (_editorReady && _editor is not null)
            await _editor.SetValue(tab.Text);
        else
            _editorNeedsSync = true;

        await InvokeAsync(StateHasChanged);

        return "Текст запроса записан в редактор активной вкладки (не выполнен).";
    }

    public async Task<string> Save()
    {
        if (activeTab is null) return "На странице запросов нет активной вкладки.";

        await RunActiveTabAsync();

        var tab = activeTab;

        if (tab?.Result is { } result)
            return result.Ok
                ? $"Запрос выполнен. Строк: {result.Rows.Length}"
                  + (result.Total is null ? "." : $", всего записей у источника: {result.Total}.")
                : $"Ошибка выполнения: {result.Message}";

        return tab?.Error is { } error ? $"Ошибка выполнения: {error}" : "Запрос выполнен.";
    }
}
