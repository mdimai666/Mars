using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using HandlebarsDotNet;
using HandlebarsDotNet.Extension.Json;
using HandlebarsDotNet.Extension.NewtonsoftJson;
using Mars.Host.Shared.TemplateEngine;

namespace Mars.TemplateEngine.Providers.HandlebarsProvider;

[Display(Name = "Handlebars", Description = "Handlebars Template Engine")]
public class HandlebarsTemplateEngine : ITemplateEngine
{
    public const string Id = "Core.Handlebars";
    string ITemplateEngine.Id => Id;

    private readonly ConcurrentDictionary<string, CachedTemplateItem> _cache = new();

    private class CachedTemplateItem
    {
        public required string TemplateHash { get; set; }
        public required HandlebarsTemplate<object, object> Compiled { get; set; }
    }

    public string Render(string template, object context)
    {
        var compiledTemplate = CreateTemplate(template);

        return compiledTemplate(context);
    }

    public string RenderCached(string id, string template, object context)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Идентификатор шаблона (Id) не может быть пустым.", nameof(id));
        if (string.IsNullOrEmpty(template)) return template;

        string currentHash = template.GetHashCode().ToString();

        if (_cache.TryGetValue(id, out var existingItem) && existingItem.TemplateHash == currentHash)
        {
            return existingItem.Compiled(context);
        }

        var finalItem = _cache.AddOrUpdate(
            id,
            key => new CachedTemplateItem
            {
                TemplateHash = currentHash,
                Compiled = CreateTemplate(template)
            },
            (key, oldItem) => oldItem.TemplateHash == currentHash
                ? oldItem
                : new CachedTemplateItem
                {
                    TemplateHash = currentHash,
                    Compiled = CreateTemplate(template)
                }
        );

        return finalItem.Compiled(context);
    }

    private HandlebarsTemplate<object, object> CreateTemplate(string template)
    {
        var configuration = new HandlebarsConfiguration
        {
            //NoEscape = true // Отключает HTML-кодирование для всех шаблонов в этом экземпляре
            TextEncoder = new HtmlEncoder() //Убирает экранирование кириллицы
        };
        var handlebars = Handlebars.Create(configuration);
        handlebars.Configuration.UseJson();
        handlebars.Configuration.UseNewtonsoftJson();
        return handlebars.Compile(template);
    }

    public bool RemoveFromCache(string id)
    {
        return _cache.TryRemove(id, out _);
    }

    public void ClearCache()
    {
        _cache.Clear();
    }
}
