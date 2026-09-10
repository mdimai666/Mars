namespace Mars.Plugin.Contracts.Catalog;

public record MarketplaceStatusResponse
{
    public required bool Enabled { get; init; }
}
