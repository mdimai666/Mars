using HandlebarsDotNet;
using Mars.SiteEngine.Handlebars.Extensions;
using Mars.TemplateEngine.Providers.HandlebarsProvider;

namespace Mars.SiteEngine.Tests;

public static class SiteHandlebarsTestFactory
{
    public static IHandlebarsEngineFactory CreateFactory() =>
        new HandlebarsEngineFactory([new SiteBasicHelpersContributor(), new SiteContextHelpersContributor()]);

    public static IHandlebars CreateSiteHandlebars() =>
        CreateFactory().Create(HandlebarsScopes.Site);
}
