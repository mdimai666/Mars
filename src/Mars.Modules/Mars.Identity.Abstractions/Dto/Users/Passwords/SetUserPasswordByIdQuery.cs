namespace Mars.Identity.Abstractions.Dto.Users.Passwords;

public record SetUserPasswordByIdQuery
{
    public required Guid UserId { get; init; }
    public required string NewPassword { get; init; }
}
