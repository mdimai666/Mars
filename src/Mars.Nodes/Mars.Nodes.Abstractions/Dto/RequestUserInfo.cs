namespace Mars.Nodes.Host.Shared.Dto;

public record RequestUserInfo
{
    public required bool IsAuthenticated { get; init; }
    public required string? UserName { get; init; }
    public required Guid? UserId { get; init; }
}
