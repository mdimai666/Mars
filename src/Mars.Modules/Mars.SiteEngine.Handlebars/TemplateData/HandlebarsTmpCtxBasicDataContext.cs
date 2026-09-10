using Mars.Core.Extensions;
using Mars.SiteEngine.Abstractions.WebSite.Models;

namespace Mars.SiteEngine.Handlebars.TemplateData;

public class HandlebarsTmpCtxBasicDataContext : ITemplateContextVariablesFiller
{
    public const string UserParamKey = "_user";
    public const string RequestParamKey = "_req";
    public const string SiteSettingsParamKey = "SiteSettings";
    public const string IsDevelopmentParamKey = "_dev";

    public const string BodyClassParamKey = "bodyClass";
    public const string BodyAttrsParamKey = "bodyAttrs";

    public const string MarsAppHeaderKey = "mars-app";
    public const string MauiPlatformHeaderKey = "maui-platform";
    public const string MauiIdiomHeaderKey = "maui-idiom";

    public const string MauiParamKey = "$maui";
    public const string MauiPlatformParamKey = "$maui_platform";
    public const string MauiIdiomParamKey = "$maui_idiom";

    public void FillTemplateDictionary(PageRenderContext pageContext, Dictionary<string, object?> templateContextVariables)
    {
        templateContextVariables.Add(UserParamKey, pageContext.User);
        templateContextVariables.Add(RequestParamKey, pageContext.Request);
        templateContextVariables.Add(SiteSettingsParamKey, pageContext.SiteSettings);
        templateContextVariables.Add(IsDevelopmentParamKey, pageContext.IsDevelopment);

        if (pageContext.User is not null)
        {
            pageContext.BodyClass.Add("logged-in");

            if (pageContext.User?.Roles.Contains("Admin") ?? false)
            {
                pageContext.BodyClass.Add("admin");
            }
        }

        templateContextVariables.Add(BodyClassParamKey, pageContext.BodyClass.JoinStr(" "));
        templateContextVariables.Add(BodyAttrsParamKey, pageContext.BodyAttrs.JoinStr(" "));

        if (pageContext.Request.Headers.TryGetValue(MarsAppHeaderKey, out var bapp))
        {
            if (bapp == "maui")
            {
                templateContextVariables.Add(MauiParamKey, true);
            }
        }

        if (pageContext.Request.Headers.TryGetValue(MauiPlatformHeaderKey, out var mauiPLatform))
        {
            templateContextVariables.Add(MauiPlatformParamKey, mauiPLatform);
        }

        if (pageContext.Request.Headers.TryGetValue(MauiIdiomHeaderKey, out var mauiIdiom))
        {
            templateContextVariables.Add(MauiIdiomParamKey, mauiIdiom);
        }

    }
}
