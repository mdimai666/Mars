using HandlebarsDotNet;
using HandlebarsDotNet.Extension.Json;
using HandlebarsDotNet.Extension.NewtonsoftJson;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Services;
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

    [Obsolete]
    public static Dictionary<Guid, MetaRelationObjectDict> FillData(ICollection<MetaValueDto> metaValues, IServiceProvider sp)
    {
        //IMetaModelTypesLocator mlocator = sp.GetRequiredService<IMetaModelTypesLocator>();

        //var list = metaValues.Where(s => s.MetaField.IsTypeRelation && s.ModelId != Guid.Empty)
        //    //distinct after fix
        //    .DistinctBy(s => s.ModelId).Select(s => new MetaRelationObjectDict(s));

        //if (list.Count() == 0) return new Dictionary<Guid, MetaRelationObjectDict>();

        //Dictionary<Guid, MetaRelationObjectDict> dict = list.ToDictionary(s => s.ModelId);
        //var grouped = list.GroupBy(s => mlocator.GetModelType(s.Type, s.ModelName));
        //foreach (var group in grouped)
        //{
        //    Type t = group.Key;
        //    //IQueryable<IBasicEntity> query = MarsDbContext.DbSetByType(t, q.ef, sp);
        //    IQueryable<IBasicEntity> query = mlocator.GetModelQueryable(sp, t.Name);
        //    var ids = group.Select(s => s.ModelId);
        //    var items = query.Where(s => ids.Contains(s.Id)).ToList();
        //    foreach (var a in items)
        //    {
        //        dict[a.Id].Entity = a;
        //    }
        //}
        //return dict;
        throw new NotImplementedException();
        /*see*/
        _ = nameof(IMetaModelTypesLocator.AllMetaRelationsStructure);
    }

    public static List<Action<IHandlebars, Dictionary<Guid, MetaRelationObjectDict>, Func<ICollection<MetaValueDto>, IServiceProvider, Dictionary<Guid, MetaRelationObjectDict>>>> extraRegisteredActions = [];

    public void RegisterContextFunctions()
    {
        //var sp = renderContext.ServiceProvider;
        // -> var renderContext = options.Data[ChainSegment.Create("rctx")] as IRenderContext;

        Dictionary<Guid, MetaRelationObjectDict> data = null;

        foreach (var x in extraRegisteredActions)
        {
            x(handlebars, data, FillData);
        }

        //handlebars.RegisterHelper("display1", (output, context, args) =>
        //{
        //    data ??= FillData(q, q.post.MetaValues);
        //    string html = TemplatorFormOutput.RenderMetaFields(q.post.MetaValues, data, q.req);
        //    output.WriteSafeString(html);
        //});

        //handlebars.RegisterHelper("edit1", (output, context, args) =>
        //{
        //    data ??= FillData(q, q.post.MetaValues);
        //    string html = TemplatorFormOutput.RenderMetaFields(q.post.MetaValues, data, q.req, edit: true);
        //    output.WriteSafeString(html);
        //});

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
