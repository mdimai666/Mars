namespace Mars.Shared.Contracts.Posts;

public record PostJsonResponse : PostSummaryResponse
{
    public required string? Content { get; init; }
    public required Dictionary<string, object?> Meta { get; init; }

}
