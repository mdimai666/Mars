namespace Mars.Cms.Abstractions.Dto.Posts;

public record ListAllPostQuery
{
    public required string? Type { get; init; }
    public IReadOnlyCollection<Guid>? Ids { get; init; }
}
