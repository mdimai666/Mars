using Mars.Cms.Contracts.Posts;

namespace Mars.Cms.Contracts.PostJsons;

public record PostJsonResponse : PostSummaryResponse
{
    public required string? Content { get; init; }

    public required IReadOnlyDictionary<string, object?> Meta { get; init; }

}
