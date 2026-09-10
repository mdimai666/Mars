using Mars.Plugin.Contracts.Catalog;
using Mars.Plugin.Contracts.Plugins;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Pages.PluginViews;

public partial class MarketplacePage
{
    private const int PageSize = 12;

    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;

    bool _statusLoading = true;
    bool _catalogEnabled;
    bool _loading;
    bool _recommendedOnly;
    string? _initError;
    string? _errorMessage;
    string _searchText = string.Empty;
    string _sort = "downloads";
    int _page = 1;
    int _total;
    List<CatalogPluginDto> _plugins = [];
    Dictionary<string, string> _installedVersions = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> _installing = new(StringComparer.OrdinalIgnoreCase);

    protected override Task OnInitializedAsync() => InitAsync();

    async Task InitAsync()
    {
        _statusLoading = true;
        _initError = null;
        StateHasChanged();

        try
        {
            var status = await client.Plugin.MarketplaceStatus();
            _catalogEnabled = status.Enabled;
        }
        catch (Exception ex)
        {
            _catalogEnabled = false;
            _initError = ex.Message;
        }
        finally
        {
            _statusLoading = false;
        }

        if (_catalogEnabled)
        {
            await RefreshInstalledAsync();
            await LoadAsync();
        }
    }

    Task ReloadAsync()
    {
        _page = 1;
        return LoadAsync();
    }

    Task LoadMoreAsync()
    {
        _page++;
        return LoadAsync(append: true);
    }

    async Task LoadAsync(bool append = false)
    {
        if (!_catalogEnabled) return;

        _loading = true;
        StateHasChanged();

        try
        {
            var response = await client.Plugin.MarketplaceSearch(new MarketplaceSearchRequest
            {
                Q = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
                Recommended = _recommendedOnly ? true : null,
                Sort = _sort,
                Page = _page,
                Take = PageSize,
            });

            _errorMessage = null;
            _total = response.Total;
            if (append)
                _plugins.AddRange(response.Items);
            else
                _plugins = [.. response.Items];
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    Task RetryAsync() => LoadAsync();

    async Task RefreshInstalledAsync()
    {
        try
        {
            var list = await client.Plugin.List(new ListPluginQueryRequest { Take = 1000 });
            _installedVersions = list.Items
                .GroupBy(p => p.PackageId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Version, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _ = _messageService.Error(ex.Message);
        }
    }

    bool IsInstalled(CatalogPluginDto plugin) => _installedVersions.ContainsKey(plugin.PackageId);

    bool HasUpdate(CatalogPluginDto plugin)
        => _installedVersions.TryGetValue(plugin.PackageId, out var installed)
           && IsNewer(installed, plugin.LatestVersion);

    static bool IsNewer(string? installed, string? latest)
    {
        if (string.IsNullOrEmpty(installed) || string.IsNullOrEmpty(latest)) return false;
        if (Version.TryParse(Strip(installed), out var current) && Version.TryParse(Strip(latest), out var available))
            return available > current;
        return !string.Equals(installed, latest, StringComparison.OrdinalIgnoreCase);
    }

    static string Strip(string version) => version.Split('+')[0].Split('-')[0];

    async Task InstallAsync(CatalogPluginDto plugin)
    {
        if (!_installing.Add(plugin.PackageId)) return;

        try
        {
            var result = await client.Plugin.InstallFromNuget(plugin.PackageId);
            _ = _messageService.Success(result.Message);
            await RefreshInstalledAsync();
        }
        catch (Exception ex)
        {
            _ = _messageService.Error(ex.Message);
        }
        finally
        {
            _installing.Remove(plugin.PackageId);
            StateHasChanged();
        }
    }

    async Task OpenDetails(CatalogPluginDto plugin)
    {
        await MarketplacePluginDialog.ShowAsync(dialogService, plugin);
        await RefreshInstalledAsync();
    }
}
