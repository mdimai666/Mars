namespace Mars.CodeCompletion.Contracts.Dto;

public class CompletionResponseDto
{
    public IReadOnlyList<CompletionItemDto> Items { get; set; } = [];
}

public class CompletionItemDto
{
    public string Label { get; set; } = "";

    /// <summary>Имя вида Monaco CompletionItemKind: Method, Function, Property, Class, Keyword, …</summary>
    public string Kind { get; set; } = "Text";

    public string? InsertText { get; set; }

    public string? FilterText { get; set; }

    public string? SortText { get; set; }

    /// <summary>Короткая подпись справа от элемента (обычно тип/сигнатура).</summary>
    public string? Detail { get; set; }

    public string? Documentation { get; set; }
}
