using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;
using Mars.Host.Shared.TemplateEngine;

namespace Mars.TemplateEngine.Host.InternalProviders;

[Display(Name = "Text Replace", Description = "Text Replace Template Engine")]
public class TextReplaceTemplateEngine : ITemplateEngine
{
    public const string Id = "Core.RegexVarReplacer";
    string ITemplateEngine.Id => Id;

    // Регулярное выражение ищет {{ключ}} или {ключ}
    private const string PropertyPattern = @"\{\{([\w]+)\}\}|\{([\w]+)\}";

    // Кэш хранит скомпилированный Regex для конкретного шаблона
    private readonly ConcurrentDictionary<string, CachedTemplateItem> _cache = new();

    private class CachedTemplateItem
    {
        public required string TemplateHash { get; set; }
        public required Regex Compiled { get; set; }
    }

    public string Render(string template, object context)
    {
        // Для одиночного вызова используем RegexOptions.None
        var regex = new Regex(PropertyPattern, RegexOptions.None);
        return ExecuteReplace(regex, template, context);
    }

    public string RenderCached(string id, string template, object context)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id не может быть пустым", nameof(id));
        if (string.IsNullOrEmpty(template)) return template;

        string currentHash = template.GetHashCode().ToString();

        // Happy-path: быстро проверяем совпадение текста, чтобы не вызывать AddOrUpdate
        if (_cache.TryGetValue(id, out var existingItem) && existingItem.TemplateHash == currentHash)
        {
            return ExecuteReplace(existingItem.Compiled, template, context);
        }

        // Для кэшируемых шаблонов используем RegexOptions.Compiled, 
        // это компилирует регулярное выражение в MSIL код и работает максимально быстро при повторных вызовах.

        // Если шаблона нет или текст изменился — перезаписываем элемент в кэше
        var finalItem = _cache.AddOrUpdate(
            id,
            // Создание новой записи, если ключа нет
            key => new CachedTemplateItem
            {
                TemplateHash = currentHash,
                Compiled = new Regex(PropertyPattern, RegexOptions.Compiled)
            },
            // Замена старой записи на новую, если текст изменился
            (key, oldItem) => oldItem.TemplateHash == currentHash
                ? oldItem
                : new CachedTemplateItem
                {
                    TemplateHash = currentHash,
                    Compiled = new Regex(PropertyPattern, RegexOptions.Compiled)
                }
        );

        return ExecuteReplace(finalItem.Compiled, template, context);
    }

    private string ExecuteReplace(Regex regex, string template, object context)
    {
        if (context == null) return template;

        // Быстро вычитываем свойства объекта в словарь через рефлексию
        var properties = GetPropertyValues(context);

        return regex.Replace(template, match =>
        {
            // Обработка экранирования: если нашли {{key}}, возвращаем {key}
            if (match.Value.StartsWith("{{"))
            {
                return match.Value.Substring(1, match.Value.Length - 2);
            }

            // Берем имя ключа из второй группы захвата (индекс 2)
            string key = match.Groups[2].Value;

            // Подставляем значение, если ключ найден в объекте, иначе оставляем {key} как есть
            return properties.TryGetValue(key, out string? value) ? value : match.Value;
        });
    }

    // Вспомогательный метод для перевода свойств объекта в строковый словарь
    private Dictionary<string, string> GetPropertyValues(object context)
    {
        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (context == null) return dictionary;

        // Если передали уже готовый словарь
        if (context is IDictionary<string, string> dict)
        {
            return new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
        }

        // Если передали анонимный объект или класс — читаем его свойства
        PropertyInfo[] properties = context.GetType().GetProperties();
        foreach (var prop in properties)
        {
            var val = prop.GetValue(context);
            dictionary[prop.Name] = val?.ToString() ?? string.Empty;
        }

        return dictionary;
    }

    public bool RemoveFromCache(string id) => _cache.TryRemove(id, out _);
    public void ClearCache() => _cache.Clear();
}
