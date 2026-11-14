using Mars.Admin.Framework.Interfaces;
using Mars.Plugin.Contracts.Catalog;
using Mars.Plugin.Contracts.Plugins;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.TechSpace.Pages.PluginViews;

public partial class TechMarketplacePage
{
    private const int PageSize = 6;

    // Витрина маленькая: один запрос на всё, фильтры/полки/карусель — на клиенте.
    private const int FetchTake = 100;

    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] IMessageService messageService { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;

    private sealed record RatingOption(double Value, string Label);

    private static readonly RatingOption[] RatingOptions =
    [
        new(0, "Any rating"),
        new(4, "4★ & up"),
        new(4.5, "4.5★ & up"),
    ];

    bool _statusLoading = true;
    bool _catalogEnabled;
    string? _initError;
    string? _loadError;
    List<CatalogPluginDto>? _items;
    Dictionary<string, string> _installedVersions = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> _installing = new(StringComparer.OrdinalIgnoreCase);

    // иконка может не отдаться (404) — буква вместо битой картинки
    readonly HashSet<string> _iconFailed = new(StringComparer.OrdinalIgnoreCase);

    int _slide;
    string _search = "";
    string _category = "all";
    double _minRating;
    readonly List<string> _activeTags = [];
    int _page = 1;

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
            await Task.WhenAll(RefreshInstalledAsync(), LoadCatalogAsync());
        }
    }

    async Task LoadCatalogAsync()
    {
        try
        {
            var response = await client.Plugin.MarketplaceSearch(new MarketplaceSearchRequest { Take = FetchTake });
            _items = [.. response.Items.OrderByDescending(p => p.TotalDownloads)];
            _loadError = null;
        }
        catch (Exception ex)
        {
            _loadError = ex.Message;
        }
        finally
        {
            StateHasChanged();
        }
    }

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
            _ = messageService.Error(ex.Message);
        }
    }

    List<CatalogPluginDto> Popular => _items!.Take(5).ToList();

    CatalogPluginDto Current => Popular[Math.Clamp(_slide, 0, Popular.Count - 1)];

    IEnumerable<(string Title, List<CatalogPluginDto> Items)> Shelves()
    {
        var recommended = _items!.Where(p => p.IsRecommended).Take(4).ToList();
        if (recommended.Count > 0)
        {
            yield return ("Recommended", recommended);
        }

        yield return ("Popular", _items!.Take(4).ToList());
    }

    List<string> Categories => _items!
        .Select(p => p.Category)
        .Where(c => !string.IsNullOrWhiteSpace(c))
        .Select(c => c!)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.OrdinalIgnoreCase)
        .ToList();

    int CountFor(string category) =>
        category == "all" ? _items!.Count : _items!.Count(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase));

    IEnumerable<string> AllTags() =>
        _items!.SelectMany(p => p.Tags).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).Take(10);

    IEnumerable<CatalogPluginDto> Filtered()
    {
        var text = _search.Trim();
        return _items!.Where(p =>
        {
            if (_category != "all" && !string.Equals(p.Category, _category, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (text.Length > 0
                && !p.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase)
                && !(p.AuthorName?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
                && !(p.Summary?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                return false;
            }

            if (p.AvgRating < _minRating)
            {
                return false;
            }

            return _activeTags.Count == 0 || _activeTags.Any(tag => p.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
        });
    }

    int PageCount() => Math.Max(1, (int)Math.Ceiling(Filtered().Count() / (double)PageSize));

    IEnumerable<CatalogPluginDto> PageItems() =>
        Filtered().Skip((_page - 1) * PageSize).Take(PageSize);

    void SetCategory(string category)
    {
        _category = category;
        _page = 1;
    }

    void SetRating(double value)
    {
        _minRating = value;
        _page = 1;
    }

    void ToggleTag(string tag)
    {
        if (!_activeTags.Remove(tag))
        {
            _activeTags.Add(tag);
        }

        _page = 1;
    }

    void ResetFilters()
    {
        _category = "all";
        _search = "";
        _minRating = 0;
        _activeTags.Clear();
        _page = 1;
    }

    void Prev() => _slide = (_slide - 1 + Popular.Count) % Popular.Count;

    void Next() => _slide = (_slide + 1) % Popular.Count;

    bool IsInstalled(CatalogPluginDto plugin) => _installedVersions.ContainsKey(plugin.PackageId);

    bool HasUpdate(CatalogPluginDto plugin)
        => _installedVersions.TryGetValue(plugin.PackageId, out var installed)
           && IsNewer(installed, plugin.LatestVersion);

    async Task InstallAsync(CatalogPluginDto plugin)
    {
        if (!_installing.Add(plugin.PackageId)) return;

        try
        {
            var result = await client.Plugin.InstallFromNuget(plugin.PackageId);
            _ = messageService.Success(result.Message);
            await RefreshInstalledAsync();
        }
        catch (Exception ex)
        {
            _ = messageService.Error(ex.Message);
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

    static bool IsNewer(string? installed, string? latest)
    {
        if (string.IsNullOrEmpty(installed) || string.IsNullOrEmpty(latest)) return false;
        if (Version.TryParse(Strip(installed), out var current) && Version.TryParse(Strip(latest), out var available))
            return available > current;
        return !string.Equals(installed, latest, StringComparison.OrdinalIgnoreCase);
    }

    static string Strip(string version) => version.Split('+')[0].Split('-')[0];

    static string CategoryLabel(string? category) =>
        string.IsNullOrWhiteSpace(category)
            ? "Plugin"
            : char.ToUpperInvariant(category[0]) + category[1..];

    static string Rating(CatalogPluginDto plugin) => plugin.AvgRating.ToString("0.0");

    static string Downloads(long count) =>
        count >= 1000 ? (count / 1000.0).ToString("0.#") + "k" : count.ToString();

    static char Initial(CatalogPluginDto plugin) =>
        string.IsNullOrEmpty(plugin.DisplayName) ? '?' : plugin.DisplayName[0];
}