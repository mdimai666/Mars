using Mars.Host.Shared.WebSite.Models;

namespace Mars.WebSiteProcessor.Handlebars.TemplateData;

public interface ITemplateContextVariblesFiller
{
    void FillTemplateDictionary(PageRenderContext pageContext, Dictionary<string, object?> templateContextVaribles);
}
