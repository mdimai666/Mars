namespace Mars.Identity.Abstractions.Dto.Auth;

public record AuthResultDto
{
    public bool IsAuthSuccessful => ErrorMessage == null;
    public required string? ErrorMessage { get; init; }
    public required string? Token { get; init; }
    public required long ExpiresIn { get; init; }
    public required string? RefreshToken { get; init; }

    public static AuthResultDto InvalidDataResponse()
        => new()
        { ErrorMessage = "Неверные данные", ExpiresIn = 0, Token = null, RefreshToken = null };

    public static AuthResultDto ErrorResponse(string errorMessage)
        => new()
        { ErrorMessage = errorMessage, ExpiresIn = 0, Token = null, RefreshToken = null };
}
