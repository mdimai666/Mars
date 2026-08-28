using System.ComponentModel.DataAnnotations;
using Mars.Server.Abstractions.Models;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Contracts.Options;
using Mars.SiteEngine.Abstractions.WebSite;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SiteEngine.Handlebars;

[Display(Name = "Handlebars", Description = "Рендер фронта из Handlebars-шаблонов в папке")]
public class HandlebarsRenderEngineFactory : IWebRenderEngineFactory
{
    public string Id => FrontItem.HandlebarsEngine;

    public IWebRenderEngine Create(MarsAppFront appFront, IServiceProvider services)
    {
        var engine = ActivatorUtilities.CreateInstance<HandlebarsWebRenderEngine>(services, appFront);
        engine.Setup();
        engine.InitializeEngine(services);

        return engine;
    }
}
