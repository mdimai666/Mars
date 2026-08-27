namespace Mars.Host.Shared.Dto.Posts;

public record PostAuthor
{
    public required Guid Id { get; init; }
    public required string? UserName { get; init; }
    public required string DisplayName { get; init; }
}
