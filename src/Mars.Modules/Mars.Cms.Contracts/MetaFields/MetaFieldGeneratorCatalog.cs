namespace Mars.Cms.Contracts.MetaFields;

/// <summary>
/// Каталог доступных генераторов значений мета-полей (общий для сервера и админки).
/// Генератор автозаполняет значение при создании объекта; настройка хранится в <c>Options.generator</c>.
/// </summary>
public static class MetaFieldGeneratorCatalog
{
    /// <summary>Порядковый номер: префикс + число с паддингом, счётчик с режимами сброса</summary>
    public const string Sequence = "sequence";

    /// <summary>Текущая дата/время: автозаполнение моментом создания</summary>
    public const string Now = "now";

    /// <summary>Режим счётчика (параметр mode генератора sequence): единый продолжающийся счётчик</summary>
    public const string ModeContinue = "continue";

    /// <summary>Режим счётчика (параметр mode генератора sequence): счётчик сбрасывается каждый день</summary>
    public const string ModeDaily = "daily";

    /// <summary>Ключ и название для UI выбора генератора</summary>
    public static IReadOnlyCollection<(string Key, string Title)> All { get; } =
    [
        (Sequence, "Порядковый номер"),
        (Now, "Текущая дата/время"),
    ];
}
