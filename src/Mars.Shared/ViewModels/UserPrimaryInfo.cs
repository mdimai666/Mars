namespace Mars.Shared.ViewModels;

public record UserPrimaryInfo
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string? Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public required string? AvatarUrl { get; init; }
}
