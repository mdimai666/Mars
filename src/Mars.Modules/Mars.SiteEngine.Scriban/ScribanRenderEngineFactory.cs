using System.ComponentModel.DataAnnotations;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Abstractions.WebSite;
using Mars.SiteEngine.Contracts.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SiteEngine.Scriban;

[Display(Name = "Scriban", Description = "Рендер фронта из Scriban-шаблонов (*.sbn) в папке")]
public class ScribanRenderEngineFactory : IWebRenderEngineFactory
{
    public string Id => FrontItem.ScribanEngine;

    public IWebRenderEngine Create(MarsAppFront appFront, IServiceProvider services)
    {
        var engine = ActivatorUtilities.CreateInstance<ScribanWebRenderEngine>(services, appFront);
        engine.Setup();

        return engine;
    }
}
