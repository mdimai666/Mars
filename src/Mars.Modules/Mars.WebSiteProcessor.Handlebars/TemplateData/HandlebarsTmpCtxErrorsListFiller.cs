using Mars.Host.Shared.WebSite.Models;

namespace Mars.WebSiteProcessor.Handlebars.TemplateData;

public class HandlebarsTmpCtxErrorsListFiller : ITemplateContextVariblesFiller
{
    public const string ErrorsParamKey = "$errors";

    public void FillTemplateDictionary(PageRenderContext pageContext, Dictionary<string, object?> templateContextVaribles)
    {
        templateContextVaribles.Add(ErrorsParamKey, pageContext.Errors);

    }
}
