using System.ComponentModel;
using Mars.AiChat.Contracts.Dto;
using Mars.AiChat.Host.Services;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструменты агента для работы с ОТКРЫТОЙ страницей админки (сейчас — редактирование поста).
/// Выполняются на клиенте через AiChatPageBridge (SignalR round-trip).
/// Экземпляр создаётся на каждый запуск агента с конкретным chatId.
/// </summary>
public class MarsOpenPageTools
{
    private readonly AiChatPageBridge _bridge;
    private readonly Guid _chatId;

    public MarsOpenPageTools(AiChatPageBridge bridge, Guid chatId)
    {
        _bridge = bridge;
        _chatId = chatId;
    }

    [Description("Узнать, какая страница сейчас открыта у пользователя в админ-панели " +
                 "(например, страница редактирования поста) и какие поля на ней доступны. " +
                 "Вызывай перед использованием инструментов открытой страницы.")]
    public async Task<string> GetOpenPageInfo()
    {
        var result = await _bridge.CallPageAsync(_chatId, "get_open_page_info");
        return Format(result);
    }

    [Description("Прочитать текущие значения полей на открытой странице редактирования поста: " +
                 "название (Title), slug, анонс (Excerpt), теги, категории и текст (Content). " +
                 "Для текста возвращается и исходный контент, и его plain-text извлечение (contentText).")]
    public async Task<string> GetOpenPageFields()
    {
        var result = await _bridge.CallPageAsync(_chatId, "get_open_page_fields");
        return Format(result);
    }

    [Description("Изменить поле на открытой странице редактирования поста (без сохранения — пользователь проверит и сохранит сам). " +
                 "Поддерживаемые поля: title, slug, excerpt, tags (через запятую), categories (Guid категорий через запятую), content. " +
                 "Для content передавай НОВЫЙ полный текст: для редактора блоков он будет разбит на абзацы.")]
    public async Task<string> SetOpenPageField(
        [Description("Имя поля: title / slug / excerpt / tags / categories / content")] string field,
        [Description("Новое значение поля")] string value)
    {
        var result = await _bridge.CallPageAsync(_chatId, "set_open_page_field", new { field, value });
        return Format(result);
    }

    [Description("Сохранить открытую страницу редактирования поста (эквивалент кнопки «Сохранить»). " +
                 "Используй ТОЛЬКО если пользователь явно попросил сохранить.")]
    public async Task<string> SaveOpenPage()
    {
        var result = await _bridge.CallPageAsync(_chatId, "save_open_page");
        return Format(result);
    }

    static string Format(AiPageToolResult result)
        => result.Ok ? result.Result : $"Ошибка страницы: {result.Result}";
}
