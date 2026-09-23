namespace Mars.CodeCompletion.Contracts.Dto;

public class CompletionResponseDto
{
    public IReadOnlyList<CompletionItemDto> Items { get; set; } = [];
}

public class CompletionItemDto
{
    public string Label { get; set; } = "";

    /// <summary>Monaco CompletionItemKind (1=Text … 25=TypeParameter).</summary>
    public int Kind { get; set; } = 1;

    public string? InsertText { get; set; }

    public string? FilterText { get; set; }

    public string? SortText { get; set; }

    /// <summary>Короткая подпись справа от элемента (обычно тип/сигнатура).</summary>
    public string? Detail { get; set; }

    public string? Documentation { get; set; }
}
