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
    volatile BakedRoutes routes;
    FrontItem? adminFront;

    public event Action? Changed;

    public FrontManager(IOptionService optionService, IEventManager eventManager, IWebHostEnvironment env)
    {
        this.optionService = optionService;
        this.env = env;
        (snapshot, routes) = Bake(optionService.GetOption<FrontsOption>());

        eventManager.AddEventListener(eventManager.Defaults.OptionUpdate(nameof(FrontsOption)), _ =>
        {
            (snapshot, routes) = Bake(optionService.GetOption<FrontsOption>());
            Changed?.Invoke();
        });
    }

    /// <summary>
    /// Запечённая таблица маршрутов: пересобирается только при изменении FrontsOption,
    /// чтение на каждый запрос без локов и перебора выключенных фронтов.
    /// </summary>
    sealed record BakedRoutes(FrontItem? Root, FrontItem[] Mounts);

    static (FrontsOption Snapshot, BakedRoutes Routes) Bake(FrontsOption option)
    {
        FrontItem? root = null;
        List<FrontItem>? mounts = null;

        foreach (var front in option.Fronts)
        {
            if (!front.Enabled) continue;

            if (string.IsNullOrEmpty(front.Url))
            {
                root ??= front;
                continue;
            }

            (mounts ??= []).Add(front);
        }

        // по убыванию длины Url: первое совпадение = наиболее специфичный маунт
        var mountsArray = mounts is null
            ? []
            : mounts.OrderByDescending(s => s.Url.Length).ToArray();

        return (option, new BakedRoutes(root, mountsArray));
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
        var r = routes;

        // hotpath: типичная установка — один корневой фронт, маунтов нет
        if (r.Mounts.Length == 0)
            return r.Root;

        // записи уже в нижнем регистре (сеттер FrontItem.Url)
        url = url.ToLowerInvariant();

        foreach (var mount in r.Mounts)
        {
            if (url.StartsWith(mount.Url)
                && (url.Length == mount.Url.Length || url[mount.Url.Length] == '/'))
                return mount;
        }

        return r.Root;
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
