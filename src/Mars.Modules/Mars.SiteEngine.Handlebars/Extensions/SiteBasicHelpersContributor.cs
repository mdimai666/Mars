using HandlebarsDotNet;
using Mars.SiteEngine.Handlebars.HandlebarsFunc;
using Mars.TemplateEngine.Providers.HandlebarsProvider;
using static Mars.SiteEngine.Handlebars.HandlebarsFunc.MyHandlebarsBasicFunctions;

namespace Mars.SiteEngine.Handlebars.Extensions;

/// <summary>
/// Бессостоятельные хелперы сайт-движка: условия, даты, текст, циклы, site_head/site_footer, help.
/// </summary>
public class SiteBasicHelpersContributor : IHandlebarsBuilderContributor
{
    public string? Scope => HandlebarsScopes.Site;

    public void Configure(IHandlebars handlebars)
    {
        handlebars.RegisterHelper("eq", EqualBlock);
        handlebars.RegisterHelper("neq", NotEqualBlock);
        handlebars.RegisterHelper("gt", GreaterThanBlock);
        handlebars.RegisterHelper("gte", GreaterThanOrEqualBlock);
        handlebars.RegisterHelper("lt", LessThanBlock);
        handlebars.RegisterHelper("lte", LessThanOrEqualBlock);

        handlebars.RegisterHelper("eqstr", EqualStringBlock);
        handlebars.RegisterHelper("neqstr", NotEqualStringBlock);
        handlebars.RegisterHelper("if_divided_by", if_divided_by_Block); // Печатает содержимое в блоке если делится на count
        handlebars.RegisterHelper("and", AndBlock);
        handlebars.RegisterHelper("or", OrBlock);
        handlebars.RegisterHelper("IsEmpty", IsEmptyBlock);
        handlebars.RegisterHelper("Contains", ContainsBlock);

        //format all DateTime type in template
        var format = "yyyy.MM.dd HH:mm";
        var formatter = new CustomDateTimeFormatter(format);
        handlebars.Configuration.FormatterProviders.Add(formatter);

        //date format
        handlebars.RegisterHelper("dateFormat", DateFormatHelper);
        handlebars.RegisterHelper("date", DateHelper);
        //handlebars.RegisterHelper("date_relative", DateRelativeHelper);
        handlebars.RegisterHelper("parsedateandformat", ParseDateandFormatHelper);

        //text helpers
        handlebars.RegisterHelper("text_excerpt", TextExcerptHelper);
        handlebars.RegisterHelper("text_ellipsis", TextEllipsisHelper);
        handlebars.RegisterHelper("nl2br", nl2br_Helper);
        handlebars.RegisterHelper("youtubeId", youtubeId_Helper);
        handlebars.RegisterHelper("ToHumanizedSize", ToHumanizedSize);

        //html processing
        handlebars.RegisterHelper("striphtml", StripHtmlHelper);
        handlebars.RegisterHelper("encode", EncodeHelper);
        handlebars.RegisterHelper("tojson", ToJsonHelper);

        //loops
        handlebars.RegisterHelper("for", ForLoopBlock); //{{#for @start @end @step? }}

        //scripts
        handlebars.RegisterHelper("site_head", MyHandlebarsSiteParts.WriteSiteHeadScripts);
        handlebars.RegisterHelper("site_footer", MyHandlebarsSiteParts.WriteSiteFooterScripts);

        //helper
        handlebars.RegisterHelper("help", HelpHelper);
    }
}
