using HandlebarsDotNet;
using Mars.TemplateEngine.Providers.HandlebarsProvider;
using static Mars.SiteEngine.Handlebars.HandlebarsFunc.MyHandlebarsContextFunctions;

namespace Mars.SiteEngine.Handlebars.Extensions;

/// <summary>
/// Контекстные хелперы сайт-движка (нужен rctx в options.Data):
/// mobile, #context (QueryLang), локализация, raw_block, iff, RenderPostContent.
/// Регистрируются один раз на экземпляр движка — контекст передаётся на каждый рендер через options.Data.
/// </summary>
public class SiteContextHelpersContributor : IHandlebarsBuilderContributor
{
    public string? Scope => HandlebarsScopes.Site;

    public void Configure(IHandlebars handlebars)
    {
        handlebars.RegisterHelper("mobile", MobileBlock);
        handlebars.RegisterHelper("!mobile", NotMobileBlock);

        handlebars.RegisterHelper("context", ContextBlock);

        handlebars.RegisterHelper("L", Localizer_Helper);
        handlebars.RegisterHelper("raw_block", RawBlock);
        handlebars.RegisterHelper("iff", IffBlock);
        handlebars.RegisterHelper("RenderPostContent", RenderPostContent);
    }
}
