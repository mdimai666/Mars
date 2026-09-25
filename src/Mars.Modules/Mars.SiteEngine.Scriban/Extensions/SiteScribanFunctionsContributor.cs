using Mars.Server.Abstractions.Interfaces;
using Mars.TemplateEngine.Providers.ScribanProvider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Mars.SiteEngine.Scriban.Extensions;

/// <summary>
/// Регистрирует сайт-функции Scriban (scope "site") в глобальном ScriptObject движка.
/// </summary>
public class SiteScribanFunctionsContributor : IScribanObjectContributor
{
    public string? Scope => ScribanScopes.Site;

    public void Configure(ScriptObject scriptObject)
    {
        // QueryLang
        scriptObject.Import("context", new Func<TemplateContext, string, string?, string?, object?>(SiteScribanFunctions.Context));

        // локализация (vararg)
        scriptObject.SetValue("L", new LocalizerFunction(), true);

        // условия через XInterpreter
        scriptObject.Import("iff", new Func<TemplateContext, string, object?>(SiteScribanFunctions.Iff));

        // справочник функций
        scriptObject.Import("help", new Func<TemplateContext, object?>(SiteScribanFunctions.Help));

        // части сайта
        scriptObject.Import("raw_block", new Func<TemplateContext, string, object?>(SiteScribanFunctions.RawBlock));
        scriptObject.Import("render_post_content", new Func<TemplateContext, object?, object?>(SiteScribanFunctions.RenderPostContent));
        scriptObject.Import("site_head", new Func<TemplateContext, object?>(SiteScribanFunctions.SiteHead));
        scriptObject.Import("site_footer", new Func<TemplateContext, object?>(SiteScribanFunctions.SiteFooter));

        // текст/html
        scriptObject.Import("text_excerpt", new Func<TemplateContext, string?, int, object?>(SiteScribanFunctions.TextExcerpt));
        scriptObject.Import("text_ellipsis", new Func<TemplateContext, string?, int, object?>(SiteScribanFunctions.TextEllipsis));
        scriptObject.Import("nl2br", new Func<TemplateContext, string?, object?>(SiteScribanFunctions.Nl2Br));
        scriptObject.Import("youtube_id", new Func<TemplateContext, string?, object?>(SiteScribanFunctions.YoutubeId));
        scriptObject.Import("striphtml", new Func<TemplateContext, string?, object?>(SiteScribanFunctions.StripHtml));
        scriptObject.Import("encode", new Func<TemplateContext, string?, object?>(SiteScribanFunctions.Encode));
        scriptObject.Import("tojson", new Func<TemplateContext, object?, object?>(SiteScribanFunctions.ToJson));
        scriptObject.Import("to_humanized_size", new Func<TemplateContext, object?, object?>(SiteScribanFunctions.ToHumanizedSize));

        // даты
        scriptObject.Import("date_format", new Func<TemplateContext, object?, string?, object?>(SiteScribanFunctions.DateFormat));
        scriptObject.Import("parsedateandformat", new Func<TemplateContext, string?, string, string?, object?>(SiteScribanFunctions.ParseDateAndFormat));
    }

    /// <summary>
    /// {{ L "string_key" values... }} — локализация, как L-хелпер Handlebars.
    /// </summary>
    class LocalizerFunction : IScriptCustomFunction
    {
        public int RequiredParameterCount => 1;
        public int ParameterCount => 16;
        public ScriptVarParamKind VarParamKind => ScriptVarParamKind.Direct;
        public Type ReturnType => typeof(string);

        public ScriptParameterInfo GetParameterInfo(int i) => i == 0
            ? new ScriptParameterInfo(typeof(string), "key")
            : new ScriptParameterInfo(typeof(object), $"arg{i}");

        public object? Invoke(TemplateContext context, ScriptNode callerNode, ScriptArray arguments, ScriptBlockStatement? blockStatement)
        {
            if (arguments.Count < 1)
            {
                throw new InvalidOperationException("L function must have at least 1 argument (string key)");
            }

            var rctx = ScribanRenderContext.From(context);

            IAppFrontLocalizer afl = rctx.ServiceProvider.GetService<IAppFrontLocalizer>()
                ?? throw new InvalidOperationException("Localizer not found: (af.Path, \"Resources\", \"AppRes.resx\")");

            IStringLocalizer localizer = afl.GetLocalizer();

            string stringKey = arguments[0]?.ToString()!;

            if (arguments.Count == 1)
            {
                return localizer[stringKey];
            }

            var formatArgs = arguments.Skip(1).Select(a => a).ToArray();
            return localizer[stringKey, formatArgs!];
        }

        public ValueTask<object?> InvokeAsync(TemplateContext context, ScriptNode callerNode, ScriptArray arguments, ScriptBlockStatement? blockStatement)
            => new(Invoke(context, callerNode, arguments, blockStatement));
    }
}
