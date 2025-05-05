namespace Mars.Host.Shared.Dto.Users.Passwords;

public record SetUserPasswordQuery
{
    public required string Username { get; init; }
    public required string NewPassword { get; init; }
}
