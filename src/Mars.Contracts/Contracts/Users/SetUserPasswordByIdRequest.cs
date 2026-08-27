namespace Mars.Shared.Contracts.Users;

public record SetUserPasswordByIdRequest
{
    public required Guid UserId { get; init; }
    public required string NewPassword { get; init; }
}
