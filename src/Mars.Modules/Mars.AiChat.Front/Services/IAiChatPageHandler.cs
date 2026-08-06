namespace Mars.AiChat.Front.Services;

/// <summary>
/// Обработчик инструментов ИИ-агента на открытой странице админки.
/// Страница (например, EditPostView) реализует интерфейс и регистрирует себя
/// в AiChatPageHandlerHolder при появлении, снимает регистрацию при уходе.
/// </summary>
public interface IAiChatPageHandler
{
    /// <summary>Информация об открытой странице: тип, идентификаторы, доступные поля (JSON).</summary>
    string GetInfo();

    /// <summary>Текущие значения полей страницы (JSON).</summary>
    Task<string> GetFields();

    /// <summary>Установить значение поля. Возвращает текст результата для агента.</summary>
    Task<string> SetField(string field, string value);

    /// <summary>Сохранить страницу (эквивалент кнопки «Сохранить»). Возвращает текст результата.</summary>
    Task<string> Save();
}

/// <summary>
/// Статический реестр текущего обработчика страницы (одна активная страница редактирования).
/// Паттерн тот же, что у AiChatAppService / AIToolAppService.
/// </summary>
public static class AiChatPageHandlerHolder
{
    public static IAiChatPageHandler? Current { get; set; }
}
