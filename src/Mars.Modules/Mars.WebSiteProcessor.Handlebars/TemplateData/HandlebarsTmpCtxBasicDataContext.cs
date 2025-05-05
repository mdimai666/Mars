using Mars.Host.Shared.WebSite.Models;

namespace Mars.WebSiteProcessor.Handlebars.TemplateData;

public class HandlebarsTmpCtxBasicDataContext : ITemplateContextVariblesFiller
{
    public const string UserParamKey = "_user";
    public const string RequestParamKey = "_req";
    public const string SysOptionsParamKey = "SysOptions";
    public const string IsDevelopmentParamKey = "_dev";

    public const string BodyClassParamKey = "bodyClass";
    public const string BodyAttrsParamKey = "bodyAttrs";

    public const string MarsAppHeaderKey = "mars-app";
    public const string MauiPlatformHeaderKey = "maui-platform";
    public const string MauiIdiomHeaderKey = "maui-idiom";

    public const string MauiParamKey = "$maui";
    public const string MauiPlatformParamKey = "$maui_platform";
    public const string MauiIdiomParamKey = "$maui_idiom";


    public void FillTemplateDictionary(PageRenderContext pageContext, Dictionary<string, object?> templateContextVaribles)
    {
        templateContextVaribles.Add(UserParamKey, pageContext.User);
        templateContextVaribles.Add(RequestParamKey, pageContext.Request);
        templateContextVaribles.Add(SysOptionsParamKey, pageContext.SysOptions);
        templateContextVaribles.Add(IsDevelopmentParamKey, pageContext.IsDevelopment);

        if (pageContext.User is not null)
        {
            pageContext.BodyClass.Add("logged-in");

            if (pageContext.User?.Roles.Contains("Admin") ?? false)
            {
                pageContext.BodyClass.Add("admin");
            }
        }

        templateContextVaribles.Add(BodyClassParamKey, pageContext.BodyClass);
        templateContextVaribles.Add(BodyAttrsParamKey, pageContext.BodyAttrs);

        if (pageContext.Request.Headers.TryGetValue(MarsAppHeaderKey, out var bapp))
        {
            if (bapp == "maui")
            {
                templateContextVaribles.Add(MauiParamKey, true);
            }
        }

        if (pageContext.Request.Headers.TryGetValue(MauiPlatformHeaderKey, out var mauiPLatform))
        {
            templateContextVaribles.Add(MauiPlatformParamKey, mauiPLatform);
        }

        if (pageContext.Request.Headers.TryGetValue(MauiIdiomHeaderKey, out var mauiIdiom))
        {
            templateContextVaribles.Add(MauiIdiomParamKey, mauiIdiom);
        }

    }
}
