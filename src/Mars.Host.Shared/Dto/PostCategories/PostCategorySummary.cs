namespace Mars.Host.Shared.Dto.PostCategories;

public class PostCategorySummary
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? ModifiedAt { get; init; }
    public required string Title { get; init; }
    public required string Type { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required string PostType { get; init; }

    /// <summary>
    /// /{rootId}/{parentId}/{id}/
    /// </summary>
    public required string Path { get; init; } = default!;

    /// <summary>
    /// /news/tech/ai
    /// </summary>
    public required string SlugPath { get; init; } = default!;
    public required Guid[] PathIds { get; init; } = [];
    public required int LevelsCount { get; init; }
}
