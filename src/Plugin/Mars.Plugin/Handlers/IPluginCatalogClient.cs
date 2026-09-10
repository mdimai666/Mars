using Mars.Plugin.Contracts.Catalog;

namespace Mars.Plugin.Handlers;

/// <summary>
/// Чтение витрины внешнего каталога плагинов. Когда каталог выключен или пакет
/// в нём отсутствует (404 каталога), методы возвращают <see langword="null"/>;
/// сбой связи/ошибка каталога — <see cref="Mars.Core.Exceptions.UserActionException"/>
/// с текстом для пользователя. Каталог не является точкой отказа Марса: установка
/// по nuget-id работает и без него.
/// </summary>
internal interface IPluginCatalogClient
{
    /// <summary>Каталог включён в настройках и задан его URL.</summary>
    bool IsEnabled { get; }

    Task<CatalogPagedResponse<CatalogPluginDto>?> SearchAsync(
        MarketplaceSearchRequest query, string? marsVersion, CancellationToken cancellationToken);

    Task<CatalogPluginDto?> GetAsync(string packageId, CancellationToken cancellationToken);

    Task<CatalogPagedResponse<CatalogReviewDto>?> GetReviewsAsync(
        string packageId, int? page, int? take, CancellationToken cancellationToken);
}
