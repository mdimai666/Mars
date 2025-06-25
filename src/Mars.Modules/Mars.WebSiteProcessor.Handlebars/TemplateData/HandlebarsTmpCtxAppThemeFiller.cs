using Mars.Host.Shared.WebSite.Models;

namespace Mars.WebSiteProcessor.Handlebars.TemplateData;

public class HandlebarsTmpCtxAppThemeFiller : ITemplateContextVariblesFiller
{
    public const string AppThemeParamKey = "appTheme";
    public const string AppThemeCookiesKey = "AppTheme";
    public const string BodyAttrAppThemeTagName = "app-theme";
    public const string BodyClassDarkThemeValue = "dark-theme";

    public void FillTemplateDictionary(PageRenderContext pageContext, Dictionary<string, object?> templateContextVaribles)
    {
        var Request = pageContext.Request;

        string? appTheme = null;

        if (Request.Cookies.TryGetValue(AppThemeCookiesKey, out var userAppTheme))
        {
            appTheme = userAppTheme;
            pageContext.BodyAttrs.Add($"{BodyAttrAppThemeTagName}=\"{appTheme}\"");

            var isDarkTheme = (userAppTheme == "dark");

            if (isDarkTheme)
            {
                pageContext.BodyClass.Add(BodyClassDarkThemeValue);
            }
        }
    }
}
