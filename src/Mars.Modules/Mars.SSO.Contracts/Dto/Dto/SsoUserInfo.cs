using Mars.Identity.Contracts.ViewModels;

namespace Mars.SSO.Contracts.Dto;

public class SsoUserInfo
{
    public required Guid InternalId { get; set; } = default!;
    public string ExternalId { get; set; } = default!;
    public string? Email { get; set; } = default!;
    public string? Name { get; set; }
    public string Provider { get; set; } = default!;
    public required string AccessToken { get; init; }

    public required UserPrimaryInfo UserPrimaryInfo { get; init; }
}
