using Mars.Host.Shared.Models;

namespace Mars.WebSiteProcessor.Interfaces;

/// <summary>
/// Фабрика движков рендера фронта. Регистрируется в DI (IEnumerable) — встроенные и плагинные.
/// Метаданные для админки — через [Display(Name, Description)] на классе.
/// </summary>
public interface IWebRenderEngineFactory
{
    string Id { get; }

    IWebRenderEngine Create(MarsAppFront appFront, IServiceProvider services);
}
