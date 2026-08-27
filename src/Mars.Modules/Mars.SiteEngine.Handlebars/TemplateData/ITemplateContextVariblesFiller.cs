using Mars.SiteEngine.Abstractions.WebSite.Models;

namespace Mars.SiteEngine.Handlebars.TemplateData;

public interface ITemplateContextVariblesFiller
{
    void FillTemplateDictionary(PageRenderContext pageContext, Dictionary<string, object?> templateContextVaribles);
}
