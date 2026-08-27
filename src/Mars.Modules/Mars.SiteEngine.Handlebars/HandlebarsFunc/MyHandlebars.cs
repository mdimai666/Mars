using HandlebarsDotNet;
using HandlebarsDotNet.Extension.Json;
using HandlebarsDotNet.Extension.NewtonsoftJson;
using Mars.Host.Shared.Templators;
using static Mars.Host.Shared.Templators.IMarsHtmlTemplator;
using static Mars.Host.Templators.HandlebarsFunc.MyHandlebarsBasicFunctions;
using static Mars.Host.Templators.HandlebarsFunc.MyHandlebarsContextFunctions;

namespace Mars.Host.Templators.HandlebarsFunc;

public class MyHandlebars : IMarsHtmlTemplator
{
    public IHandlebars handlebars;

    public MyHandlebars()
    {
        handlebars = Handlebars.Create();
        handlebars.Configuration.UseJson();
        handlebars.Configuration.UseNewtonsoftJson();

        //see for all features BuildInHelpersFeatureFactory

        handlebars.RegisterHelper("eq", EqualBlock);
        handlebars.RegisterHelper("neq", NotEqualBlock);
        handlebars.RegisterHelper("gt", GreaterThanBlock);
        handlebars.RegisterHelper("gte", GreaterThanOrEqualBlock);
        handlebars.RegisterHelper("lt", LessThanBlock);
        handlebars.RegisterHelper("lte", LessThanOrEqualBlock);

        //=========================================================
        handlebars.RegisterHelper("eqstr", EqualStringBlock);
        handlebars.RegisterHelper("neqstr", NotEqualStringBlock);
        handlebars.RegisterHelper("if_divided_by", if_divided_by_Block); // Печатает содержимое в блоке если делится на count
        handlebars.RegisterHelper("and", AndBlock); // Печатает содержимое в блоке если делится на count
        handlebars.RegisterHelper("or", OrBlock); // Печатает содержимое в блоке если делится на count
        handlebars.RegisterHelper("IsEmpty", IsEmptyBlock); // Печатает содержимое в блоке если делится на count
        handlebars.RegisterHelper("Contains", ContainsBlock); // Печатает содержимое в блоке если делится на count
        //=========================================================

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

    public void RegisterContextFunctions()
    {
        //var sp = renderContext.ServiceProvider;
        // -> var renderContext = options.Data[ChainSegment.Create("rctx")] as IRenderContext;

        handlebars.RegisterHelper("mobile", MobileBlock);
        handlebars.RegisterHelper("!mobile", NotMobileBlock);

        handlebars.RegisterHelper("context", ContextBlock);

        handlebars.RegisterHelper("L", Localizer_Helper);
        handlebars.RegisterHelper("raw_block", RawBlock);
        handlebars.RegisterHelper("iff", IffBlock);
        handlebars.RegisterHelper("RenderPostContent", RenderPostContent);
    }

    public static TimeSpan? ParseStringTimespan(string simespanString)
    {
        string[] formats = { @"m\m", @"h\h\m\m", @"s\s" };
        TimeSpan ts;
        if (TimeSpan.TryParseExact(simespanString, formats, null, out ts))
        {
            return ts;
        }
        else
        {
            return null;
        }
    }

    public MarsHtmlTemplate<object, object> Compile(string template)
    {
        var templator = handlebars.Compile(template);
        return (context, data) => templator(context, data);
    }

    public void RegisterTemplate(string templateName, string template)
    {
        handlebars.RegisterTemplate(templateName, template);
    }

    public void Dispose()
    {
        handlebars.Configure().Dispose();
    }
}
