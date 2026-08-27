namespace Mars.Host.Shared.Dto.Posts;

public record PostStatusDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required string Color { get; init; }
    public required int Order { get; init; }
}
