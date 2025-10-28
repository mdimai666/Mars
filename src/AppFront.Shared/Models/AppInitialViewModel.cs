using System.Text.Json.Serialization;
using Mars.Shared.Common;
using Mars.Shared.Contracts.NavMenus;
using Mars.Shared.Contracts.Options;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Contracts.Renders;
using Mars.Shared.Contracts.XActions;
using Mars.Shared.Options;
using Mars.Shared.ViewModels;

namespace AppFront.Shared.Models;

public class AppInitialViewModel
{
    public SysOptions SysOptions { get; set; } = new();
    public required UserPrimaryInfo? InitailUserPrimaryInfo { get; set; }
    public List<NavMenuDetailResponse> NavMenus { get; set; } = [];
    public IReadOnlyCollection<PostTypeSummaryResponse> PostTypes { get; set; } = [];

    [JsonIgnore]
    public RenderActionResult<PostRenderResponse>? IndexPage => LocalPages.FirstOrDefault(s => s.Data?.PostSlug == "index");
    public List<RenderActionResult<PostRenderResponse>> LocalPages { get; set; } = [];
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
