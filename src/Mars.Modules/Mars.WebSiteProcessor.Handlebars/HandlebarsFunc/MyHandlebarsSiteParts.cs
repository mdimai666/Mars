using HandlebarsDotNet;
using Mars.Host.Constants.Website;
using Mars.Host.Shared.Templators;
using Mars.Host.Shared.WebSite.Scripts;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Templators.HandlebarsFunc;

public static class MyHandlebarsSiteParts
{
    [TemplatorHelperInfo("site_head", "{{{#site_head}}}", "Writes HTML  headers meta tags, scripts e.t.c.")]
    public static void WriteSiteHeadScripts(in EncodedTextWriter output, in HelperOptions options, in Context context, in Arguments arguments)
    {
        var renderContext = options.Data.RenderContext();
        var serviceProvider = renderContext.ServiceProvider;
        var appFrontBuilder = serviceProvider.GetRequiredKeyedService<ISiteScriptsBuilder>(AppFrontConstants.SiteScriptsBuilderKey);

        output.WriteSafeString(appFrontBuilder.HeadScriptsRender());
    }

    [TemplatorHelperInfo("site_footer", "{{{#site_footer}}}", "Writes HTML footer scripts.")]
    public static void WriteSiteFooterScripts(in EncodedTextWriter output, in HelperOptions options, in Context context, in Arguments arguments)
    {
        var renderContext = options.Data.RenderContext();
        var serviceProvider = renderContext.ServiceProvider;
        var appFrontBuilder = serviceProvider.GetRequiredKeyedService<ISiteScriptsBuilder>(AppFrontConstants.SiteScriptsBuilderKey);

        output.WriteSafeString(appFrontBuilder.FooterScriptsRender());
    }
}
