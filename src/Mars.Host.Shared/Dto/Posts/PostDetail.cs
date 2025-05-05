using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Shared.Dto.Posts;

public record PostDetail : PostSummary
{
    public required string? Content { get; init; }

    public required IReadOnlyCollection<MetaValueDto> MetaValues { get; init; }
}

public record PostEditDetail 
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? ModifiedAt { get; init; }
    public required string Title { get; init; }
    public required string Type { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required PostAuthor Author { get; init; }
    public required string Status { get; init; }
    public required string? Content { get; init; }
    public required string? Excerpt { get; init; }
    public required string LangCode { get; init; }

    public required IReadOnlyCollection<MetaValueDetailDto> MetaValues { get; init; }
}
