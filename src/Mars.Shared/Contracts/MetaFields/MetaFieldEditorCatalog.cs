namespace Mars.Shared.Contracts.MetaFields;

/// <summary>
/// Каталог доступных редакторов значений мета-полей (общий для сервера и админки).
/// Выбор редактора поля хранится в <c>Options.editor</c>; пусто = дефолтный редактор типа.
/// Реестр «ключ → компонент + совместимые типы» — на фронте (<c>IMetaFieldEditorLocator</c>).
/// </summary>
public static class MetaFieldEditorCatalog
{
    /// <summary>Выбор цвета (для полей String)</summary>
    public const string Color = "color";

    /// <summary>URL-адрес (для полей String)</summary>
    public const string Url = "url";

    /// <summary>Email (для полей String)</summary>
    public const string Email = "email";

    /// <summary>Дата (для полей DateTime)</summary>
    public const string Date = "date";

    /// <summary>Время (для полей DateTime)</summary>
    public const string Time = "time";

    /// <summary>Дата и время (для полей DateTime)</summary>
    public const string DateTime = "datetime";

    /// <summary>Ключ и название для UI выбора редактора</summary>
    public static IReadOnlyCollection<(string Key, string Title)> All { get; } =
    [
        (Color, "Цвет"),
        (Url, "URL-адрес"),
        (Email, "Email"),
        (DateTime, "Дата и время"),
        (Date, "Дата"),
        (Time, "Время"),
    ];
}
