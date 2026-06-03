using System.ComponentModel.DataAnnotations;
using Mars.Host.Shared.TemplateEngine;

namespace Mars.TemplateEngine.Host.InternalProviders;

[Display(Name = "Plain Text", Description = "Plain Text Template")]
public class PlainTextTemplateEngine : ITemplateEngine
{
    public const string Id = "Core.PlainText";
    string ITemplateEngine.Id => Id;

    public string Render(string template, object context)
    {
        return template;
    }

    public string RenderCached(string id, string template, object context)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id не может быть пустым", nameof(id));

        return template;
    }

    public bool RemoveFromCache(string id) => false;
    public void ClearCache() { }
}
