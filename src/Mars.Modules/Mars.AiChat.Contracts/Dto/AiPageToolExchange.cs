namespace Mars.AiChat.Shared.Dto;

/// <summary>
/// Запрос сервера клиенту: выполнить инструмент на открытой странице админки.
/// </summary>
public class AiPageToolRequest
{
    public string RequestId { get; set; } = "";

    /// <summary>Имя инструмента: get_open_page_info / get_open_page_fields / set_open_page_field / save_open_page.</summary>
    public string Tool { get; set; } = "";

    /// <summary>JSON аргументов инструмента.</summary>
    public string ArgsJson { get; set; } = "";
}

/// <summary>
/// Ответ клиента серверу: результат инструмента на открытой странице.
/// </summary>
public class AiPageToolResult
{
    public string RequestId { get; set; } = "";
    public bool Ok { get; set; }
    public string Result { get; set; } = "";
}
