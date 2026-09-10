namespace Mars.Plugin.Contracts.Catalog;

/// <summary>
/// Параметры поиска по витрине каталога. Совместимость с текущей версией Марса
/// (minVersion) подставляется сервером, клиент её не задаёт.
/// </summary>
public record MarketplaceSearchRequest
{
    public string? Q { get; init; }

    public string? Tag { get; init; }

    public bool? Recommended { get; init; }

    /// <summary>downloads | rating | newest (по умолчанию — downloads).</summary>
    public string? Sort { get; init; }

    public int? Page { get; init; }

    public int? Take { get; init; }
}
