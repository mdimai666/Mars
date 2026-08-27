namespace Mars.Host.Shared.Dto.Posts;

public record ListAllPostQuery
{
    public required string? Type { get; init; }
    public IReadOnlyCollection<Guid>? Ids { get; init; }
}
