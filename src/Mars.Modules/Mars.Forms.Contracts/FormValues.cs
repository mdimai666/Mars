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

    /// <summary>Ид владельца строкой: Guid у поста, произвольный ключ у внешней таблицы</summary>
    public string? OwnerId { get; init; }

    /// <summary>Мешок значений; <see cref="FormFieldDescriptor.Multiple"/> — массив в порядке индексов</summary>
    public Dictionary<string, JsonNode?> Values { get; init; } = [];

    /// <summary>Отсутствие ключа = значение не задано (не null-сентинел)</summary>
    public bool Has(string key) => Values.ContainsKey(key);

    public JsonNode? Value(string key) => Values.TryGetValue(key, out var node) ? node : null;
}

/// <summary>Результат отправки формы: провайдер валидирует и записывает сам</summary>
public record FormSubmitResult
{
    public bool Ok { get; init; }

    /// <summary>Ид созданной/обновлённой записи (строкой, см. <see cref="FormValues.OwnerId"/>)</summary>
    public string? Id { get; init; }

    public IReadOnlyCollection<FormError> Errors { get; init; } = [];

    public static FormSubmitResult Success(string? id = null) => new() { Ok = true, Id = id };

    public static FormSubmitResult Failed(IEnumerable<FormError> errors)
        => new() { Ok = false, Errors = errors.ToList() };
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
