using Mars.Core.Extensions;
using Mars.Host.Shared.WebSite.Models;

namespace Mars.WebSiteProcessor.Handlebars.TemplateData;

public class HandlebarsTmpCtxLanguageDataFiller : ITemplateContextVariblesFiller
{
    public const string LanguageParamKey = "_lang";

    public void FillTemplateDictionary(PageRenderContext pageContext, Dictionary<string, object?> templateContextVaribles)
    {
        var headers = pageContext.Request.Headers;

        string defaultLanguage = "ru";
        //var languages = Request.GetTypedHeaders()
        //           .AcceptLanguage
        //           ?.OrderByDescending(x => x.Quality ?? 1) // Quality defines priority from 0 to 1, where 1 is the highest.
        //           .Select(x => x.Value.ToString())
        //           .ToArray() ?? Array.Empty<string>();

        var acceptLanguage = headers.AcceptLanguage.FirstOrDefault() ?? defaultLanguage;

        //string? cookieLang = Request.Cookies["c"];
        //string? cookieLang = Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];

        var threadLang = Thread.CurrentThread.CurrentUICulture;

        string? cookieLang = threadLang?.TwoLetterISOLanguageName;

        string langTwo;

        if (string.IsNullOrEmpty(cookieLang))
        {
            langTwo = acceptLanguage.Left(2);
        }
        else
        {
            langTwo = cookieLang.Left(2);
        }

        //httpContext.Response.Cookies.Append(
        //    CookieRequestCultureProvider.DefaultCookieName,
        //    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("ru-RU")),
        //    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
        //);

        templateContextVaribles.Add(LanguageParamKey, langTwo);

    }
}
