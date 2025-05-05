namespace Mars.Shared.Contracts.Auth;

public record AuthResultResponse
{
    public bool IsAuthSuccessful => ErrorMessage == null;
    public required string? ErrorMessage { get; init; }
    public string? Token { get; init; }
    public long ExpiresIn { get; init; }
    public string? RefreshToken { get; init; }
}
