namespace Mars.Shared.Contracts.SSO;

public record SsoProviderItemResponse
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string? IconUrl { get; init; }
}
