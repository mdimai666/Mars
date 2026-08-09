using Mars.Host.Shared.Models;

namespace Mars.Host.Shared.Services;

/// <summary>
/// Фасад над IFrontManager для старых потребителей (ноды RenderPage, контроллеры рендера, CLI).
/// Источник истины по фронтам — FrontsOption; экземпляры MarsAppFront кэшируются в IWebRenderEngineLocator.
/// </summary>
public interface IMarsAppProvider //TODO: придумать что с ним делать дальше
{
    public IReadOnlyDictionary<string, MarsAppFront> Apps { get; }
    public MarsAppFront FirstApp { get; }
    public bool SetupMultiApps { get; }
    public MarsAppFront GetAppForUrl(string url);

    /// <summary>
    /// MarsAppFront по slug фронта (включая специальный фронт админки и выключенные фронты).
    /// </summary>
    public MarsAppFront GetAppBySlug(string slug);
}
