namespace Mars.Shared.Contracts.Auth;

public record RegistrationResultResponse
{
    public required bool IsSuccessfulRegistration { get; init; }
    public required IReadOnlyCollection<string> Errors { get; init; }

    /// <summary>
    /// HttpStatus codes
    /// </summary>
    public required int Code { get; init; }
}
