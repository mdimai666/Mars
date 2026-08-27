using Mars.SiteEngine.Abstractions.WebSite.Models;

namespace Mars.SiteEngine.Handlebars.TemplateData;

public class HandlebarsTmpCtxErrorsListFiller : ITemplateContextVariblesFiller
{
    public const string ErrorsParamKey = "$errors";

    public void FillTemplateDictionary(PageRenderContext pageContext, Dictionary<string, object?> templateContextVaribles)
    {
        templateContextVaribles.Add(ErrorsParamKey, pageContext.Errors);

    }
}
