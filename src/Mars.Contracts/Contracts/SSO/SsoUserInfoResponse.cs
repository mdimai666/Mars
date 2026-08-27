using Mars.Shared.ViewModels;

namespace Mars.Shared.Contracts.SSO;

public record SsoUserInfoResponse
{
    public required Guid InternalId { get; init; }
    public required string ExternalId { get; init; }
    public required string? Email { get; init; }
    public required string? Name { get; init; }
    public required string Provider { get; init; }
    public required string AccessToken { get; init; }

    public required UserPrimaryInfo UserPrimaryInfo { get; init; }
}
