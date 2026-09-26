namespace Mars.CodeCompletion.Contracts.Dto;

public class CompletionResponseDto
{
    public IReadOnlyList<CompletionItemDto> Items { get; set; } = [];

    /// <summary>Список обрезан сервером (LSP isIncomplete) — при продолжении ввода клиент
    /// должен перезапросить сервер, а не фильтровать полученное локально.</summary>
    public bool Incomplete { get; set; }
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

    /// <summary>Правки, применяемые Monaco вместе со вставкой элемента
    /// (completionItem.additionalTextEdits) — у нас: дописывание `using` для неимпортированных типов.</summary>
    public IReadOnlyList<AdditionalTextEditDto>? AdditionalTextEdits { get; set; }
}

public class AdditionalTextEditDto
{
    public int OffsetFrom { get; set; }

    public int OffsetTo { get; set; }

    public string NewText { get; set; } = "";
}
