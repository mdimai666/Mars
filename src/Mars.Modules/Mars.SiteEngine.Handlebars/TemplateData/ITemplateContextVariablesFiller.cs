using Mars.SiteEngine.Abstractions.WebSite.Models;

namespace Mars.SiteEngine.Handlebars.TemplateData;

public interface ITemplateContextVariablesFiller
{
    void FillTemplateDictionary(PageRenderContext pageContext, Dictionary<string, object?> templateContextVariables);
}
