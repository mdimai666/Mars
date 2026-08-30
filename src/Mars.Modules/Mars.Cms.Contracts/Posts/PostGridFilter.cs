namespace Mars.Cms.Contracts.Posts;

/// <summary>Операторы фильтров грида постов</summary>
public static class PostGridFilterOps
{
    /// <summary>Подстрока (без учёта регистра)</summary>
    public const string Contains = "contains";

    /// <summary>Точное равенство</summary>
    public const string Eq = "eq";

    /// <summary>Входит в набор значений</summary>
    public const string In = "in";

    /// <summary>Больше или равно (числа/даты)</summary>
    public const string Gte = "gte";

    /// <summary>Меньше или равно (числа/даты)</summary>
    public const string Lte = "lte";

    /// <summary>Значение отсутствует/пустое</summary>
    public const string Empty = "empty";

    /// <summary>Значение заполнено</summary>
    public const string NotEmpty = "notEmpty";
}

/// <summary>
/// Фильтр колонки грида постов. Ключ — базовая колонка
/// (<c>PostTypeGridConstants</c>) или ключ мета-поля типа.
/// </summary>
public record PostGridFilter
{
    public required string Key { get; init; }

    /// <summary>Оператор из <see cref="PostGridFilterOps"/></summary>
    public required string Op { get; init; }

    /// <summary>Скалярное значение (contains/eq/gte/lte)</summary>
    public string? Value { get; init; }

    /// <summary>Набор значений (in)</summary>
    public string[]? Values { get; init; }
}
