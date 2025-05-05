namespace Mars.Shared.Contracts.Search;

public record SearchFoundElementResponse
{
    public required string Title { get; init; }
    public required string Key { get; init; }
    public required string? Description { get; init; }
    public required string? Url { get; init; }
    public required float Relevant { get; init; }

    public required FoundElementType Type { get; init; }
    public required IReadOnlyDictionary<string, string> Data { get; init; }
}

public enum FoundElementType
{
    Url,
    Page,
    XAction,
    Record
}
