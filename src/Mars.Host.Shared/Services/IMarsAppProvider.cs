using Mars.Host.Shared.Models;

namespace Mars.Host.Shared.Services;

public interface IMarsAppProvider
{
    public Dictionary<string, MarsAppFront> Apps { get; }
    public MarsAppFront FirstApp { get; set; }
    public bool SetupMultiApps { get; set; }
    public MarsAppFront GetAppForUrl(string url);
}
