namespace Mars.Forms.Contracts;

/// <summary>
/// Элемент раскладки формы. Список плоский и упорядоченный: лист-поле ссылается на поле по ключу,
/// а элемент с <see cref="SectionTitle"/> открывает группу — поля идут за ним до следующего
/// маркера (как <c>Tab</c> в ACF). Вложенности у раскладки нет.
/// </summary>
public record FormItem
{
    /// <summary>Ключ поля; у маркера секции — стабильный идентификатор</summary>
    public required string Key { get; init; }

    /// <summary>Зона размещения: набор зон объявляет провайдер</summary>
    public string? Zone { get; init; }

    /// <summary>Переопределение заголовка поля (у маркера секции — <see cref="SectionTitle"/>)</summary>
    public string? Title { get; init; }

    public bool Visible { get; init; } = true;

    /// <summary>Ширина из <see cref="FormItemWidths"/> (null — на всю ширину зоны)</summary>
    public string? Width { get; init; }

    /// <summary>Заголовок секции: элемент — маркер начала группы, поля и значения у него отсутствуют</summary>
    public string? SectionTitle { get; init; }

    /// <summary>
    /// Дескриптор поля. Заполняет провайдер при отдаче определения;
    /// в сохранённой раскладке отсутствует (дескрипторы не хранятся — устареют).
    /// </summary>
    public FormFieldDescriptor? Field { get; init; }

    public bool IsSectionHeader => SectionTitle is not null;
}

/// <summary>Ширина элемента в зоне</summary>
public static class FormItemWidths
{
    public const string Full = "full";
    public const string Half = "half";
    public const string Third = "third";
}
