using System.ComponentModel.DataAnnotations;
using Mars.Host.Shared.Models;
using Mars.Shared.Options;
using Mars.WebSiteProcessor.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebSiteProcessor.Handlebars;

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
