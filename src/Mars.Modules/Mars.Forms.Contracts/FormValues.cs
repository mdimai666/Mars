using System.Text.Json.Nodes;

namespace Mars.Forms.Contracts;

/// <summary>
/// Универсальный транспорт значений формы: один мешок «ключ поля → значение» для всех
/// провайдеров. Типизация и раскладка по хранилищам — на стороне провайдера
/// (он знает, что <c>price</c> это decimal в колонке, а <c>tags</c> — список строк).
/// Кодировки значений — <see cref="FormValueCodec"/>.
/// </summary>
public record FormValues
{
    public required string OwnerModel { get; init; }

    /// <summary>Мешок значений; <see cref="FormFieldDescriptor.Multiple"/> — массив в порядке индексов</summary>
    public Dictionary<string, JsonNode?> Values { get; init; } = [];

    /// <summary>Отсутствие ключа = значение не задано (не null-сентинел)</summary>
    public bool Has(string key) => Values.ContainsKey(key);

    public JsonNode? Value(string key) => Values.TryGetValue(key, out var node) ? node : null;
}

/// <summary>Ошибка поля формы</summary>
public record FormError
{
    /// <summary>Ключ поля (для ошибок уровня формы — ключ формы)</summary>
    public required string Key { get; init; }

    /// <summary>Индекс значения в множественном поле (0 для одиночных)</summary>
    public int Index { get; init; }

    public required string Message { get; init; }
}
