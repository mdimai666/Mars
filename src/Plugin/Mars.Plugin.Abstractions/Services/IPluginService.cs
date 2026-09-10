using Mars.Contracts.Common;
using Mars.Plugin.Abstractions.Dto.Plugins;
using Mars.Plugin.Contracts.Catalog;
using Microsoft.AspNetCore.Http;

namespace Mars.Plugin.Abstractions.Services;

public interface IPluginService
{
    ListDataResult<PluginInfoDto> List(ListPluginQuery query);
    PagingResult<PluginInfoDto> ListTable(ListPluginQuery query);
    IDictionary<string, PluginManifestInfoDto> RuntimePluginManifests();
    Task<PluginsUploadOperationResultDto> UploadPlugin(IFormFileCollection files, CancellationToken cancellationToken);
    Task<PluginInstallResultDto> InstallFromNuget(string packageId, string? version, CancellationToken cancellationToken);
    Task SetEnabled(string packageId, bool enabled);
    Task Uninstall(string packageId);

    /// <summary>Включён ли внешний каталог плагинов (витрина маркетплейса).</summary>
    bool MarketplaceEnabled();

    Task<CatalogPagedResponse<CatalogPluginDto>?> SearchMarketplace(MarketplaceSearchRequest query, CancellationToken cancellationToken);
    Task<CatalogPluginDto?> GetMarketplacePlugin(string packageId, CancellationToken cancellationToken);
    Task<CatalogPagedResponse<CatalogReviewDto>?> GetMarketplaceReviews(string packageId, int? page, int? take, CancellationToken cancellationToken);
}
