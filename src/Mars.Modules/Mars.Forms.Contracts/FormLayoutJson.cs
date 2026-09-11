using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Mars.Forms.Contracts;

/// <summary>
/// Сериализация раскладки формы в json (camelCase) и обратно — по образцу
/// <c>PostTypeGridSettingsJson</c>. Хранится только раскладка: узлы, их родители, порядок, зоны,
/// видимость, ширина и тексты; дескрипторы не хранятся.
/// </summary>
public static class FormLayoutJson
{
    static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// Разбирает хранимый json. Узел дерева старого формата (вложенное свойство <c>items</c>)
    /// становится заголовком, а его дети — соседями (так раскладка выглядела до R3).
    /// Отсутствует/битый json — null.
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

            // легаси-формат: узел с детьми был секцией — теперь это её заголовок
            if (item.Items is { Count: > 0 })
            {
                target.Add(new FormItem
                {
                    Key = string.IsNullOrEmpty(item.Key) ? FormItem.NewKey(FormItemKind.Heading) : item.Key,
                    Kind = FormItemKind.Heading,
                    Zone = itemZone,
                    Title = string.IsNullOrWhiteSpace(item.Title) ? "Секция" : item.Title,
                });

                Append(target, item.Items, itemZone);
                continue;
            }

            if (string.IsNullOrEmpty(item.Key)) continue;

            // легаси-формат: маркер секции в плоском списке — тоже заголовок
            var section = !string.IsNullOrWhiteSpace(item.SectionTitle);

            target.Add(new FormItem
            {
                Key = item.Key,
                Parent = item.Parent,
                Zone = itemZone,
                Kind = section ? FormItemKind.Heading : ParseKind(item.Kind),
                Title = section ? item.SectionTitle : item.Title,
                Visible = item.Visible,
                Width = item.Width,
            });
        }
    }

    static FormItemKind ParseKind(string? kind)
        => Enum.TryParse<FormItemKind>(kind, ignoreCase: true, out var parsed) ? parsed : FormItemKind.Field;

    /// <summary>Форма хранения раскладки (<c>items</c> у элемента читается только для старого дерева)</summary>
    sealed class StoredLayout
    {
        public List<StoredItem>? Items { get; set; }
    }

    sealed class StoredItem
    {
        public string? Key { get; set; }
        public string? Parent { get; set; }
        public string? Zone { get; set; }
        public string? Kind { get; set; }
        public string? Title { get; set; }
        public string? SectionTitle { get; set; }
        public bool Visible { get; set; } = true;
        public string? Width { get; set; }
        public List<StoredItem>? Items { get; set; }
    }
}
