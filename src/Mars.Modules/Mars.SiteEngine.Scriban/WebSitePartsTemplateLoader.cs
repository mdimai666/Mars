using Mars.SiteEngine.Abstractions.WebSite.Models;
using Mars.SiteEngine.Contracts.WebSite.Models;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Mars.SiteEngine.Scriban;

/// <summary>
/// Загрузчик include-шаблонов: блоки и лейауты фронта (<see cref="WebSiteTemplate.Parts"/>).
/// </summary>
public class WebSitePartsTemplateLoader(IReadOnlyCollection<WebSitePart>? parts) : ITemplateLoader
{
    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) => templateName;

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        var part = parts?.FirstOrDefault(p =>
            p.Name == templatePath
            && (p.Type == WebSitePartType.Block || p.Type == WebSitePartType.Layout));

        if (part is null)
        {
            throw new InvalidOperationException($"ScribanWebRenderEngine: include '{templatePath}' not found in front parts (Block/Layout)");
        }

        return part.Content;
    }

    public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
        => new(Load(context, callerSpan, templatePath));
}
