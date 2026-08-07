using Mars.Host.Shared.Templators;
using Mars.Host.Templators.HandlebarsFunc;
using Mars.WebSiteProcessor.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebSiteProcessor.Handlebars;

public static class MainHandlebarsWRE
{
    public static WebApplicationBuilder AddWREHandlebars(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IMarsHtmlTemplator, MyHandlebars>();
        builder.Services.AddSingleton<IWebRenderEngineFactory, HandlebarsRenderEngineFactory>();

        return builder;
    }
}
