namespace Mars.Shared.Contracts.MetaFields;

/// <summary>
/// Каталог доступных валидаторов значений мета-полей (общий для сервера и админки).
/// Серверный реестр — <c>Mars.Host.Shared.Utils.MetaFieldValueValidators</c>, расширяемый.
/// </summary>
public static class MetaFieldValidatorCatalog
{
    public const string Regex = "regex";
    public const string Length = "length";
    public const string Unique = "unique";

    /// <summary>Ключ и название для UI выбора валидаторов</summary>
    public static IReadOnlyCollection<(string Key, string Title)> All { get; } =
    [
        (Regex, "Регулярное выражение"),
        (Length, "Длина строки"),
        (Unique, "Уникальное значение"),
    ];

    /// <summary>Правила, применимые к типу поля</summary>
    public static IReadOnlyCollection<(string Key, string Title)> For(MetaFieldType type)
    {
        var allowed = new List<string>();

        if (type is MetaFieldType.String or MetaFieldType.Text)
        {
            allowed.Add(Regex);
            allowed.Add(Length);
        }

        if (type is MetaFieldType.String or MetaFieldType.Text
                 or MetaFieldType.Int or MetaFieldType.Long or MetaFieldType.Float or MetaFieldType.Decimal
                 or MetaFieldType.DateTime)
            allowed.Add(Unique);

        return All.Where(item => allowed.Contains(item.Key)).ToList();
    }
}
