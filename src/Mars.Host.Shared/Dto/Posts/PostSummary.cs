using Mars.Host.Shared.Dto.PostCategories;
using Mars.Shared.Models.Interfaces;

namespace Mars.Host.Shared.Dto.Posts;

public record PostSummary : IBasicEntity
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? ModifiedAt { get; init; }
    public required string Title { get; init; }
    public required string Type { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required PostAuthor Author { get; init; }
    public required KeyValuePair<string, string>? Status { get; init; }

    public required IReadOnlyCollection<PostCategorySummary>? Categories { get; init; }
}
