using Mars.Shared.Contracts.Auth;

namespace Mars.Shared.Contracts.SSO;

public record AuthStepsResponse
{
    public required AuthStepAction Action { get; init; }
    public string RedirectUrl { get; init; } = "";
    public AuthResultResponse? AuthResultResponse { get; init; }
    public string? ErrorMessage { get; init; }
}

public enum AuthStepAction
{
    Redirect,
    Complete,
    Error
}
