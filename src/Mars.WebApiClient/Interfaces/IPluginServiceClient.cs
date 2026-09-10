using Mars.Contracts.Common;
using Mars.Plugin.Contracts.Catalog;
using Mars.Plugin.Contracts.Plugins;

namespace Mars.WebApiClient.Interfaces;

public interface IPluginServiceClient
{
    Task<ListDataResult<PluginInfoResponse>> List(ListPluginQueryRequest filter);
    Task<PagingResult<PluginInfoResponse>> ListTable(TablePluginQueryRequest filter);
    Task<PluginsUploadOperationResultResponse> UploadPlugin(params IReadOnlyCollection<(Stream file, string filename)> files);
    Task<PluginInstallResponse> InstallFromNuget(string packageId, string? version = null);
    Task SetEnabled(string packageId, bool enabled);
    Task Uninstall(string packageId);

    Task<MarketplaceStatusResponse> MarketplaceStatus();
    Task<CatalogPagedResponse<CatalogPluginDto>> MarketplaceSearch(MarketplaceSearchRequest filter);
    Task<CatalogPluginDto?> MarketplacePlugin(string packageId);
    Task<CatalogPagedResponse<CatalogReviewDto>> MarketplaceReviews(string packageId, int? page = null, int? take = null);
}
