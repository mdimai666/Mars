namespace Mars.Shared.Contracts.MetaFields;

/// <summary>
/// Каталог доступных валидаторов значений мета-полей (общий для сервера и админки).
/// Серверный реестр — <c>Mars.Host.Shared.Utils.MetaFieldValueValidators</c>, расширяемый.
/// </summary>
public static class MetaFieldValidatorCatalog
{
    public const string Regex = "regex";
    public const string Length = "length";

    /// <summary>Ключ и название для UI выбора валидаторов</summary>
    public static IReadOnlyCollection<(string Key, string Title)> All { get; } =
    [
        (Regex, "Регулярное выражение"),
        (Length, "Длина строки"),
    ];
}
