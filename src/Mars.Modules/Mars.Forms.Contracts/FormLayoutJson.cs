using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mars.Forms.Contracts;

/// <summary>
/// Сериализация раскладки формы в json (camelCase) и обратно — по образцу
/// <c>PostTypeGridSettingsJson</c>. Хранится только раскладка: порядок, зоны, видимость,
/// ширина и маркеры секций; дескрипторы не хранятся.
/// </summary>
public static class FormLayoutJson
{
    static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Разбирает хранимый json. Дерево секций старого формата (вложенное свойство <c>items</c>)
    /// разворачивается в плоский список с маркерами. Отсутствует/битый json — null.
    /// </summary>
    public static FormLayoutSettings? Parse(JsonNode? node)
    {
        if (node is null) return null;

        try
        {
            var stored = node.Deserialize<StoredLayout>(Options);
            if (stored is null) return null;

            var items = new List<FormItem>();
            Append(items, stored.Items ?? [], null);

            return new FormLayoutSettings { Items = items };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static JsonNode? ToJsonNode(this FormLayoutSettings? settings)
        => settings is null ? null : JsonSerializer.SerializeToNode(settings, Options);

    static void Append(List<FormItem> target, IReadOnlyCollection<StoredItem> source, string? zone)
    {
        foreach (var item in source)
        {
            var itemZone = string.IsNullOrEmpty(item.Zone) ? zone : item.Zone;

            // легаси-формат: узел с детьми был секцией — теперь это её маркер
            if (item.Items is { Count: > 0 })
            {
                target.Add(new FormItem
                {
                    Key = string.IsNullOrEmpty(item.Key) ? NewSectionKey() : item.Key,
                    Zone = itemZone,
                    SectionTitle = string.IsNullOrWhiteSpace(item.Title) ? "Секция" : item.Title,
                });

                Append(target, item.Items, itemZone);
                continue;
            }

            if (string.IsNullOrEmpty(item.Key)) continue;

            target.Add(new FormItem
            {
                Key = item.Key,
                Zone = itemZone,
                Title = item.Title,
                Visible = item.Visible,
                Width = item.Width,
                SectionTitle = item.SectionTitle,
            });
        }
    }

    static string NewSectionKey() => "section-" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>Форма хранения раскладки (<c>items</c> у элемента читается только для старого дерева)</summary>
    sealed class StoredLayout
    {
        public List<StoredItem>? Items { get; set; }
    }

    sealed class StoredItem
    {
        public string? Key { get; set; }
        public string? Zone { get; set; }
        public string? Title { get; set; }
        public string? SectionTitle { get; set; }
        public bool Visible { get; set; } = true;
        public string? Width { get; set; }
        public List<StoredItem>? Items { get; set; }
    }
}
