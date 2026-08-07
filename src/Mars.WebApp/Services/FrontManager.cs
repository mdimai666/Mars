using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Services;
using Mars.Shared.Options;

namespace Mars.Services;

public class FrontManager : IFrontManager
{
    /// <summary>
    /// Папка фронтов по умолчанию: data/fronts/&lt;slug&gt;
    /// </summary>
    public const string FrontsDirName = "fronts";

    /// <summary>
    /// Зарезервированный slug специального фронта админки.
    /// </summary>
    public const string AdminFrontSlug = "admin";

    /// <summary>
    /// Папка фронта админки относительно ContentRoot: data/admin/front
    /// </summary>
    public static string AdminFrontDirName => Path.Combine("data", "admin", "front");

    readonly IOptionService optionService;
    readonly IWebHostEnvironment env;

    volatile FrontsOption snapshot;
    FrontItem? adminFront;

    public event Action? Changed;

    public FrontManager(IOptionService optionService, IEventManager eventManager, IWebHostEnvironment env)
    {
        this.optionService = optionService;
        this.env = env;
        snapshot = optionService.GetOption<FrontsOption>();

        eventManager.AddEventListener(eventManager.Defaults.OptionUpdate(nameof(FrontsOption)), _ =>
        {
            snapshot = optionService.GetOption<FrontsOption>();
            Changed?.Invoke();
        });
    }

    public IReadOnlyList<FrontItem> Fronts => snapshot.Fronts;

    public FrontItem AdminFront => adminFront ??= new FrontItem
    {
        Slug = AdminFrontSlug,
        Title = "Admin",
        Url = "",
        Path = AdminFrontDirName,
        EngineId = FrontItem.HandlebarsEngine,
        Enabled = true,
    };

    public FrontItem? FindBySlug(string slug)
    {
        if (string.Equals(slug, AdminFrontSlug, StringComparison.OrdinalIgnoreCase))
            return AdminFront;

        return snapshot.Fronts.FirstOrDefault(s => string.Equals(s.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public FrontItem? GetFrontForUrl(string url)
    {
        //TODO: выглядит медленно, надо запечь
        // и в большинстве же случаев будет один "/" поэтому пердусмотреть hotpath
        url = url.ToLowerInvariant();

        FrontItem? rootFront = null;
        FrontItem? best = null;

        foreach (var front in snapshot.Fronts)
        {
            if (!front.Enabled) continue;

            if (string.IsNullOrEmpty(front.Url))
            {
                rootFront ??= front;
                continue;
            }

            if ((url == front.Url || url.StartsWith(front.Url + "/"))
                && (best is null || front.Url.Length > best.Url.Length))
            {
                best = front;
            }
        }

        return best ?? rootFront;
    }

    public string ResolvePhysicalPath(FrontItem front)
    {
        if (!string.IsNullOrWhiteSpace(front.Path))
        {
            return Path.IsPathRooted(front.Path)
                ? front.Path
                : Path.Combine(env.ContentRootPath, front.Path);
        }

        return Path.Combine(env.ContentRootPath, "data", FrontsDirName, front.Slug);
    }

    /// <summary>
    /// Валидный slug для папки фронта: буквы/цифры/_/- , не пустой
    /// </summary>
    public static bool IsValidSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return false;

        foreach (var ch in slug)
        {
            if (char.IsLetterOrDigit(ch) || ch is '_' or '-') continue;
            return false;
        }

        return true;
    }
}
