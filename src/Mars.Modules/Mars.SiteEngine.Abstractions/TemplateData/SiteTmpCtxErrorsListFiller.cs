using Mars.SiteEngine.Abstractions.WebSite.Models;

namespace Mars.SiteEngine.Abstractions.TemplateData;

public class SiteTmpCtxErrorsListFiller : ITemplateContextVariablesFiller
{
    public const string ErrorsParamKey = "$errors";

    public void FillTemplateDictionary(PageRenderContext pageContext, Dictionary<string, object?> templateContextVariables)
    {
        templateContextVariables.Add(ErrorsParamKey, pageContext.Errors);

    }
}
