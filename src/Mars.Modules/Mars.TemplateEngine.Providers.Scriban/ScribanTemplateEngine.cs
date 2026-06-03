using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Mars.Host.Shared.TemplateEngine;
using Scriban;
using Scriban.Runtime;

namespace Mars.TemplateEngine.Providers.ScribanProvider;

[Display(Name = "Scriban", Description = "Scriban Template Engine")]
public class ScribanTemplateEngine : ITemplateEngine
{
    public const string Id = "Core.Scriban";
    string ITemplateEngine.Id => Id;

    private readonly ConcurrentDictionary<string, CachedTemplateItem> _cache = new();

    private class CachedTemplateItem
    {
        public required string TemplateHash { get; set; }
        public required Template Compiled { get; set; }
    }

    public string Render(string template, object context)
    {
        var parsedTemplate = Template.Parse(template);
        return RenderParsed(parsedTemplate, context);
    }

    public string RenderCached(string id, string template, object context)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id не может быть пустым", nameof(id));
        if (string.IsNullOrEmpty(template)) return template;

        string currentHash = template.GetHashCode().ToString();

        if (_cache.TryGetValue(id, out var existingItem) && existingItem.TemplateHash == currentHash)
        {
            return RenderParsed(existingItem.Compiled, context);
        }

        var finalItem = _cache.AddOrUpdate(
            id,
            key => new CachedTemplateItem
            {
                TemplateHash = currentHash,
                Compiled = Template.Parse(template)
            },
            (key, oldItem) => oldItem.TemplateHash == currentHash
                ? oldItem
                : new CachedTemplateItem
                {
                    TemplateHash = currentHash,
                    Compiled = Template.Parse(template)
                }
        );

        return RenderParsed(finalItem.Compiled, context);
    }

    private string RenderParsed(Template parsedTemplate, object context)
    {
        var contextScriptObject = new ScriptObject();
        contextScriptObject.Import(context, renamer: member => member.Name);

        var templateContext = new TemplateContext
        {
            // 2. Переименование при выполнении шаблона (чтобы вложенные свойства 
            // вроде User.Name и User.Age не искали name и age в snake_case)
            MemberRenamer = member => member.Name
        };
        templateContext.PushGlobal(contextScriptObject);

        return parsedTemplate.Render(templateContext);
    }

    public bool RemoveFromCache(string id) => _cache.TryRemove(id, out _);
    public void ClearCache() => _cache.Clear();
}
