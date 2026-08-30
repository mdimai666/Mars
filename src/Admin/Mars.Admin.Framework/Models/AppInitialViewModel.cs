using Mars.Cms.Contracts.NavMenus;
using Mars.Cms.Contracts.PostTypes;
using Mars.Identity.Contracts.ViewModels;
using Mars.Options.Contracts.Dto.Options;
using Mars.Server.Contracts.Options;
using Mars.XActions.Contracts;

namespace Mars.Admin.Framework.Models;

public class AppInitialViewModel
{
    public SiteSettings SiteSettings { get; set; } = new();
    public string ServerWorkDirectory { get; set; } = "";
    public required UserPrimaryInfo? InitialUserPrimaryInfo { get; set; }
    public List<NavMenuDetailResponse> NavMenus { get; set; } = [];
    public IReadOnlyCollection<PostTypeAdminPanelItemResponse> PostTypes { get; set; } = [];
    public required IReadOnlyDictionary<string, XActionCommand> XActions { get; set; }

    List<OptionSummaryResponse> _options = [];
    public List<OptionSummaryResponse> Options { get => _options; set { _options = value; optCache.Clear(); } }

    static Dictionary<Type, object> optCache = [];

    public T? GetOption<T>() where T : class
    {
        Type t = typeof(T);
        var key = t.Name;

        lock (optCache)
        {
            if (!optCache.ContainsKey(t))
            {
                var json = Options.FirstOrDefault(s => s.Key == key)?.Value;

                if (json is null) return null;

                T val = System.Text.Json.JsonSerializer.Deserialize<T>(json) ?? throw new ArgumentException($"cannot parse key='{key}', json='{json}'");

                optCache[t] = val;
            }
        }

        return optCache[t] as T;
    }

    public T GetRequiredOption<T>() where T : class
    {
        var val = GetOption<T>();
        ArgumentNullException.ThrowIfNull(val, nameof(val));
        return val;
    }

}
