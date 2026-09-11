using System.Text.Json.Serialization;

namespace Mars.Forms.Contracts;

/// <summary>
/// Узел раскладки формы: поле, структурное звено сетки (контейнер-таб, ряд, колонка) или
/// неполевой элемент (заголовок, разделитель). Список плоский и упорядоченный: узел ссылается
/// на родителя по <see cref="Parent"/>, а порядок в списке — порядок внутри родителя.
/// </summary>
public record FormItem
{
    /// <summary>Ключ узла: у поля — ключ поля, у структурных узлов — сгенерированный</summary>
    public required string Key { get; init; }

    /// <summary>Ключ родительского узла; null — корень зоны</summary>
    public string? Parent { get; init; }

    /// <summary>Зона размещения: набор зон объявляет провайдер (у корневых узлов зоны)</summary>
    public string? Zone { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public FormItemKind Kind { get; init; } = FormItemKind.Field;

    /// <summary>Заголовок узла: переопределение у поля, текст у заголовка, имя таба у контейнера</summary>
    public string? Title { get; init; }

    public bool Visible { get; init; } = true;

    /// <summary>Ширина колонки из <see cref="FormItemWidths"/> (null — на всю ширину зоны)</summary>
    public string? Width { get; init; }

    /// <summary>
    /// Дескриптор поля. Заполняет провайдер при отдаче определения;
    /// в сохранённой раскладке отсутствует (дескрипторы не хранятся — устареют).
    /// </summary>
    public FormFieldDescriptor? Field { get; init; }

    /// <summary>Новый ключ структурного узла, которого в раскладке ещё нет</summary>
    public static string NewKey(FormItemKind kind)
        => $"{kind.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}"[..16];
}


/// <summary>Тип узла раскладки формы</summary>
public enum FormItemKind
{
    /// <summary>Поле формы</summary>
    Field,

    /// <summary>Контейнер зоны: рендерится табом, если контейнеров больше одного</summary>
    Container,

    /// <summary>Ряд сетки</summary>
    Row,

    /// <summary>Колонка ряда (ячейка) со своей шириной</summary>
    Column,

    /// <summary>Неполевой заголовок</summary>
    Heading,

    /// <summary>Неполевой разделитель</summary>
    Divider,
}

/// <summary>Ширина колонки в зоне</summary>
public static class FormItemWidths
{
    public const string Full = "full";
    public const string Half = "half";
    public const string Third = "third";
    public const string Quarter = "quarter";
}
