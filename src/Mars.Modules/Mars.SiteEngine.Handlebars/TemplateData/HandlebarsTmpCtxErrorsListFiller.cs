using Mars.SiteEngine.Abstractions.WebSite.Models;

namespace Mars.SiteEngine.Handlebars.TemplateData;

public class HandlebarsTmpCtxErrorsListFiller : ITemplateContextVariablesFiller
{
    public const string ErrorsParamKey = "$errors";

    public void FillTemplateDictionary(PageRenderContext pageContext, Dictionary<string, object?> templateContextVariables)
    {
        templateContextVariables.Add(ErrorsParamKey, pageContext.Errors);

    }
}
