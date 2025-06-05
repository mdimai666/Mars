using HandlebarsDotNet;
using Mars.Core.Extensions;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Templators;
using Mars.Host.Shared.WebSite.Models;
using Mars.WebSiteProcessor.Handlebars.Parsers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Mars.Host.Templators.HandlebarsFunc;

public static class MyHandlebarsContextFunctions
{
    public static void MobileBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length != 0)
        {
            throw new HandlebarsException("{{#mobile}} helper must have exactly 0 arguments");
        }

        var renderContext = options.Data.RenderContext();

        if (renderContext.PageContext.Request.IsMobile) options.Template(output, context);
        else options.Inverse(output, context);
    }

    public static void NotMobileBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length != 0)
        {
            throw new HandlebarsException("{{#!mobile}} helper must have exactly 0 arguments");
        }

        var renderContext = options.Data.RenderContext();

        if (!renderContext.PageContext.Request.IsMobile) options.Template(output, context);
        else options.Inverse(output, context);
    }

    public static void ContextBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        var renderContext = options.Data.RenderContext();
        var sp = renderContext.ServiceProvider;

        arguments.Hash.TryGetValue("key", out var _key);
        string? key = _key?.ToString();
        arguments.Hash.TryGetValue("cache", out var _cache);
        string? cache = _cache?.ToString();

        bool isCache = string.IsNullOrEmpty(cache) == false;
        string cacheKey = $"context.--{key}";
        IMemoryCache? memoryCache = null;

        if (isCache && string.IsNullOrEmpty(key))
        {
            throw new HandlebarsException("#context helper with 'cache' must have 'key'");
        }
        else if (isCache)
        {
            //IMemoryCache? memoryCache = null;
            //string? cachevalue = null;
            memoryCache = sp.GetRequiredService<IMemoryCache>();


            if (memoryCache.TryGetValue(cacheKey, out IEnumerable<KeyValuePair<string, object>>? vv))
            {
                //return html!;
                foreach (var x in vv)
                {
                    var currentFrameCtx = context.Value as Dictionary<string, object>;
                    currentFrameCtx.Add(x.Key, x.Value);
                }
                return;
            }
        }

        if (context.Value is Dictionary<string, object> jctx)
        {
            var body = options.Template();
            //var queryRows = renderContext.PageContext.AddDataQueriesRows(body, key);
            var queryRows = HandlebarsContextHelperFunctionBodyParser.FunctionBodyParse(body, key);

            var currentFrameCtx = (context.Value as Dictionary<string, object>)!;

            Dictionary<string, object>? dCopy = null;
            if (isCache)
            {
                dCopy = currentFrameCtx.ToDictionary(entry => entry.Key,
                                           entry => entry.Value);
            }

            var hlp = new HandlebarsContextBlockProcessor();

            if (queryRows.Queries.Any())
                renderContext.PageContext.DataQueries.Add(key ?? Guid.NewGuid().ToString(), queryRows);

            hlp.Process(renderContext).ConfigureAwait(false).GetAwaiter().GetResult();

            if (isCache && dCopy is not null)
            {
                var dResult = currentFrameCtx.ToDictionary(entry => entry.Key,
                                           entry => entry.Value);

                IEnumerable<KeyValuePair<string, object>> diff = dCopy.Except(dResult).Concat(dResult.Except(dCopy));

                var tsCache = MyHandlebars.ParseStringTimespan(cache ?? "10m");
                memoryCache?.Set(cacheKey, diff, tsCache ?? TimeSpan.FromMinutes(5));
            }

            //var arr = parseBodyStringToKeyValuePairs(body);

        }
        else
        {
            throw new Exception("'{{#context}}' support only Dictionary");
        }
    }

    public static void Localizer_Helper(in EncodedTextWriter output, in HelperOptions options, in Context context, in Arguments args)
    {
        if (args.Length < 1)
        {
            throw new HandlebarsException(" Localization: {{#L  \"string_key\" values[]?}} helper must have more 1 argument");
        }

        var renderContext = options.Data.RenderContext();
        var sp = renderContext.ServiceProvider;

        IAppFrontLocalizer? afl = sp.GetService<IAppFrontLocalizer>();
        IStringLocalizer? L = null;
        if (afl != null) L = afl.GetLocalizer();

        if (afl is null)
        {
            throw new HandlebarsException(" LocalizerNot found: (af.Path, \"Resources\", \"AppRes.resx\")");
        }

        string stringKey = args[0]?.ToString()!;

        string localized;

        if (args.Length == 1)
        {
            localized = L[stringKey];
        }
        else
        {
            localized = L[stringKey, args.Skip(1).ToArray()];
        }

        output.WriteSafeString(localized);
    }

    public static void RawBlock(in EncodedTextWriter output, in HelperOptions options, in Context context, in Arguments args)
    {
        if (args.Length != 1)
        {
            throw new HandlebarsException("{{#raw_block \"block_name\"}} helper must have exactly 1 arguments");
        }

        var renderContext = options.Data.RenderContext();

        var block_name = args[0].ToString();

        //var t = handlebars.Configuration.RegisteredTemplates[block_name];

        var template = renderContext.Features.Get<WebSiteTemplate>();

        if (template is null) throw new HandlebarsException("renderContext.Features didnt found 'WebSiteTemplate'");


        var block = template.Parts.FirstOrDefault(s => s.Name == block_name);

        if (block != null)
        {
            output.WriteSafeString(block.Content);
        }

        //if (handlebars.Configuration.RegisteredTemplates.TryGetValue(block_name, out var block))
        //{
        //    int z = 1;
        //    //output.WriteSafeString(block.)
        //}
    }

    public static void IffBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments args)
    {
        var renderContext = options.Data.RenderContext();
        if (args.Length != 1) new HandlebarsException("{{#iff 'x>y'}} helper must have exactly 1 arguments. Заверните выражение в кавычки");
        if (args[0] is HandlebarsDotNet.Compiler.HashParameterDictionary)
        {
            throw new HandlebarsException("#iff - argument is HashParameterDictionary. Заверните выражение в кавычки");
        }
        try
        {
            XInterpreter ppt = new XInterpreter(renderContext.PageContext);

            var expression = args[0].ToString();
            var par = ppt.GetParameters();
            bool cond = ppt.Get.Eval<bool>(expression, par);

            if (cond) options.Template(output, context);
            else options.Inverse(output, context);
        }
        catch (Exception ex)
        {
            //renderContext.AddError(ex.Message);
            throw new HandlebarsException("IffBlock: " + ex.Message, ex);
            //throw new HandlebarsException("Condition error", ex);
        }
    }
}
