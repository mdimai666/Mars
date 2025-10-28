namespace Mars.Host.Shared.Dto.SSO;

public record SsoProviderInfo
{
    public required string ProviderSlug { get; init; }
    public required string DisplayName { get; init; }
    public required string Driver { get; init; }

}
