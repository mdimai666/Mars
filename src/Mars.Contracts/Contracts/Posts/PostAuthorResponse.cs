namespace Mars.Shared.Contracts.Posts;

public class PostAuthorResponse
{
    public required Guid Id { get; init; }
    public required string? UserName { get; init; }
    public required string DisplayName { get; init; }
}
