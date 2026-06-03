using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;
using Mars.Host.Shared.TemplateEngine;
using Scriban;
using Scriban.Runtime;

namespace Mars.TemplateEngine.Providers.ScribanProvider;

/// <summary>
/// experimental razor style template engine
/// </summary>
[Display(Name = "ScribanRazorStyle", Description = "ScribanRazorStyle Template Engine")]
public class ScribanRazorStyleTemplateEngine : ITemplateEngine
{
    public const string Id = "Core.ScribanRazorStyle";
    string ITemplateEngine.Id => Id;

    private readonly ConcurrentDictionary<string, CachedTemplateItem> _cache = new();

    private class CachedTemplateItem
    {
        public required string TemplateHash { get; set; }
        public required Template Compiled { get; set; }
    }

    public string Render(string template, object context)
    {
        string scribanTemplateText = TranslateRazorToScriban(template);
        var parsedTemplate = Template.Parse(scribanTemplateText);

        if (parsedTemplate.HasErrors)
        {
            var errors = string.Join("; ", parsedTemplate.Messages);
            throw new InvalidOperationException($"Ошибка парсинга шаблона Scriban: {errors}\nСгенерированный синтаксис: {scribanTemplateText}");
        }

        return RenderParsed(parsedTemplate, context);
    }

    public string RenderCached(string id, string template, object context)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id не может быть пустым", nameof(id));
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
                Compiled = CompileRazorTemplate(template)
            },
            (key, oldItem) => oldItem.TemplateHash == currentHash
                ? oldItem
                : new CachedTemplateItem
                {
                    TemplateHash = currentHash,
                    Compiled = CompileRazorTemplate(template)
                }
        );

        return RenderParsed(finalItem.Compiled, context);
    }

    private Template CompileRazorTemplate(string razorTemplate)
    {
        string scribanText = TranslateRazorToScriban(razorTemplate);
        return Template.Parse(scribanText);
    }

    private string TranslateRazorToScriban(string template)
    {
        if (string.IsNullOrEmpty(template)) return template;

        // Шаг 1. Точечно переводим управляющие конструкции Razor в чистый синтаксис Scriban
        // @if(...) { -> {{ if ... }}
        string result = Regex.Replace(template, @"@if\s*\((.*?)\)\s*\{", "{{ if $1 }}");

        // } else if(...) { -> {{ else if ... }}
        result = Regex.Replace(result, @"\}\s*else\s*if\s*\((.*?)\)\s*\{", "{{ else if $1 }}");

        // } else { -> {{ else }}
        result = Regex.Replace(result, @"\}\s*else\s*\{", "{{ else }}");

        // Шаг 2. Заменяем ОСТАВШИЕСЯ одиночные закрывающие скобки } на {{ end }}
        // Проходим по строке и меняем только те }, которые не находятся внутри блоков {{ }}
        var sb = new StringBuilder();
        bool insideScribanBlock = false;

        for (int i = 0; i < result.Length; i++)
        {
            if (i < result.Length - 1 && result[i] == '{' && result[i + 1] == '{')
            {
                insideScribanBlock = true;
                sb.Append("{{");
                i++;
                continue;
            }
            if (i < result.Length - 1 && result[i] == '}' && result[i + 1] == '}')
            {
                insideScribanBlock = false;
                sb.Append("}}");
                i++;
                continue;
            }

            if (result[i] == '}' && !insideScribanBlock)
            {
                sb.Append("{{ end }}");
            }
            else
            {
                sb.Append(result[i]);
            }
        }
        result = sb.ToString();

        // Позволяет точки между словами (User.Name), но запрещает точку на конце слова перед пробелом/знаком препинания.
        result = Regex.Replace(result, @"@(?!if|else)([\w]+(?:\.[\w]+)*)", "{{ $1 }}");

        return result;
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
