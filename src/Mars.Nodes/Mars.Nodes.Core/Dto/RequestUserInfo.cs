namespace Mars.Nodes.Core.Dto;

public record RequestUserInfo
{
    public required bool IsAuthenticated { get; set; }
    public required string? UserName { get; set; }
    public required Guid? UserId { get; set; }
}
