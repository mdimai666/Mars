using Mars.Host.Shared.TemplateEngine;
using Mars.TemplateEngine.Host.InternalProviders;
using Mars.TemplateEngine.Providers.HandlebarsProvider;
using Mars.TemplateEngine.Providers.ScribanProvider;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.TemplateEngine.Host;

public static class MainMarsTemplateEngine
{
    public static IServiceCollection MarsAddTemplateEngines(this IServiceCollection services)
    {
        return services
            .AddSingleton<ITemplateManager, TemplateManager>()
            .AddSingleton<ITemplateEngine, PlainTextTemplateEngine>()
            .AddSingleton<ITemplateEngine, TextReplaceTemplateEngine>()
            .AddSingleton<ITemplateEngine, HandlebarsTemplateEngine>()
            .AddSingleton<ITemplateEngine, ScribanTemplateEngine>()
            .AddSingleton<ITemplateEngine, ScribanRazorStyleTemplateEngine>()
            ;
    }
}
