namespace Mars.Forms.Contracts;

/// <summary>
/// Элемент дерева формы — контейнер. Два встроенных вида: <see cref="FormItemKinds.Field"/>
/// (лист, ссылка на поле) и <see cref="FormItemKinds.Section"/> (чистый layout: значения нет).
/// Провайдер может объявить свои виды в манифесте.
/// </summary>
public record FormItem
{
    public string Kind { get; init; } = FormItemKinds.Field;

    /// <summary>Для field — ключ поля (системного или метаполя); для section — стабильный ид</summary>
    public required string Key { get; init; }

    /// <summary>Зона размещения (у корневых элементов): набор зон объявляет провайдер</summary>
    public string? Zone { get; init; }

    /// <summary>Заголовок секции; для field — переопределение заголовка поля</summary>
    public string? Title { get; init; }

    public bool Visible { get; init; } = true;

    /// <summary>Ширина из <see cref="FormItemWidths"/> (null — на всю ширину зоны)</summary>
    public string? Width { get; init; }

    /// <summary>Секция свёрнута</summary>
    public bool Collapsed { get; init; }

    /// <summary>Дети секции</summary>
    public IReadOnlyCollection<FormItem> Items { get; init; } = [];

    /// <summary>
    /// Дескриптор поля. Заполняет провайдер при отдаче определения;
    /// в сохранённой раскладке отсутствует (дескрипторы не хранятся — устареют).
    /// </summary>
    public FormFieldDescriptor? Field { get; init; }

    /// <summary>
    /// Правила валидации. Легаси-хранилище: до переноса параметров поля в настройки владельца
    /// (<see cref="FormFieldSettings"/>) правила системных слотов жили в раскладке. Нормализатор
    /// их больше не переносит — читаются только при материализации старых раскладок.
    /// </summary>
    public IReadOnlyCollection<FormRuleDefinition> Rules { get; init; } = [];

    /// <summary>Переопределение редактора — легаси, см. <see cref="Rules"/></summary>
    public string? Editor { get; init; }

    public bool IsSection => Kind == FormItemKinds.Section;
}

/// <summary>Встроенные виды контейнеров формы</summary>
public static class FormItemKinds
{
    /// <summary>Лист: ссылка на поле по ключу</summary>
    public const string Field = "field";

    /// <summary>Узел-группа: заголовок и дети, значения не имеет</summary>
    public const string Section = "section";

    public static readonly IReadOnlyList<string> All = [Field, Section];
}

/// <summary>Ширина элемента в зоне</summary>
public static class FormItemWidths
{
    public const string Full = "full";
    public const string Half = "half";
    public const string Third = "third";

    public static readonly IReadOnlyList<string> All = [Full, Half, Third];
}
