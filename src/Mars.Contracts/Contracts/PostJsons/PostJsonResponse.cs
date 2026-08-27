using Mars.Shared.Contracts.Posts;

namespace Mars.Shared.Contracts.PostJsons;

public record PostJsonResponse : PostSummaryResponse
{
    public required string? Content { get; init; }

    public required IReadOnlyDictionary<string, object?> Meta { get; init; }

}
