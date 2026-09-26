namespace Mars.CodeCompletion.Contracts.Dto;

public class CodePositionRequest
{
    /// <summary>Идентификатор редактора на клиенте (стабилен между запросами одной формы).</summary>
    public string DocumentId { get; set; } = "";

    /// <summary>Полный текст кода на момент запроса.</summary>
    public string Code { get; set; } = "";

    /// <summary>Смещение курсора в символах от начала текста.</summary>
    public int Offset { get; set; }
}

public class CodeCompletionInfo
{
    public bool Enabled { get; set; }

    /// <summary>Зарегистрированные contextId (slug'и контекстов кода).</summary>
    public IReadOnlyList<string> Contexts { get; set; } = [];
}
