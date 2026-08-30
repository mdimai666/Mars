using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Mars.Admin.Shared.ActionCenter;

public class RecentPage
{
    public required string Url { get; init; }
    public string? Title { get; init; }
    public DateTimeOffset Ts { get; init; }
}

/// <summary>
/// Недавние страницы админки: подписка на LocationChanged, запись в localStorage
/// (дедупликация по url, лимит), заголовок берётся из dev-меню при совпадении.
/// </summary>
public class RecentPagesService : IDisposable
{
    public const string StorageKey = "action-center.recent-pages";
    const int MaxPages = 10;

    readonly NavigationManager _navigationManager;
    readonly ILocalStorageService _localStorage;

    List<RecentPage> _pages = [];
    bool _initialized;

    public IReadOnlyList<RecentPage> Pages => _pages;

    public RecentPagesService(NavigationManager navigationManager, ILocalStorageService localStorage)
    {
        _navigationManager = navigationManager;
        _localStorage = localStorage;
    }

    public async Task EnsureInitializedAsync()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            _pages = await _localStorage.GetItemAsync<List<RecentPage>>(StorageKey) ?? [];
        }
        catch
        {
            _pages = [];
        }

        _navigationManager.LocationChanged += OnLocationChanged;
    }

    void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _ = RecordAsync(e.Location);
    }

    async Task RecordAsync(string location)
    {
        var relative = ToBaseRelative(location);
        if (string.IsNullOrWhiteSpace(relative)) return;

        var page = new RecentPage
        {
            Url = "/" + relative,
            Title = ResolveTitle(relative),
            Ts = DateTimeOffset.Now,
        };

        _pages.RemoveAll(p => p.Url == page.Url);
        _pages.Insert(0, page);
        if (_pages.Count > MaxPages)
            _pages = _pages.Take(MaxPages).ToList();

        try
        {
            await _localStorage.SetItemAsync(StorageKey, _pages);
        }
        catch
        {
            // localStorage может быть недоступен — не критично
        }
    }

    string ToBaseRelative(string url)
    {
        try
        {
            return _navigationManager.ToBaseRelativePath(url).Trim('/');
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Заголовок из dev-меню, если пункт с таким url есть; иначе null (покажется url).
    /// </summary>
    static string? ResolveTitle(string relativeUrl)
    {
        var devMenu = Q.Site?.NavMenus?.FirstOrDefault(m => m.Slug == "dev");
        if (devMenu is null) return null;

        var item = devMenu.MenuItems
            .FirstOrDefault(i => !string.IsNullOrEmpty(i.Url) && i.Url.Trim('/').Equals(relativeUrl, StringComparison.OrdinalIgnoreCase));

        return item?.Title;
    }

    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
