using Mars.SiteEngine.Abstractions.Templators;
using Mars.SiteEngine.Handlebars.HandlebarsFunc;
using Mars.SiteEngine.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SiteEngine.Handlebars;

public static class MainHandlebarsWRE
{
    public static WebApplicationBuilder AddWREHandlebars(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IMarsHtmlTemplator, MyHandlebars>();
        builder.Services.AddSingleton<IWebRenderEngineFactory, HandlebarsRenderEngineFactory>();

        return builder;
    }
}
