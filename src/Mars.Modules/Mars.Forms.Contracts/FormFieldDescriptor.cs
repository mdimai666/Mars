using System.Text.Json.Nodes;

namespace Mars.Forms.Contracts;

public record FormFieldDescriptor
{
    public required string Key { get; init; }

    public required string Title { get; init; }

    /// <summary>
    /// Ключ ресурса заголовка (например <c>Title</c> из <c>AppRes</c>), если заголовок переводимый;
    /// клиент резолвит его своим локализатором, а <see cref="Title"/> остаётся фолбэком.
    /// Для пользовательских полей пусто — их заголовок литеральный.
    /// </summary>
    public string? TitleKey { get; init; }

    public required FormFieldType Type { get; init; }

    public bool Required { get; init; }

    public bool ReadOnly { get; init; }

    /// <summary>Поле допускает несколько значений: значение-массив, порядок = индекс</summary>
    public bool Multiple { get; init; }

    public string? Description { get; init; }

    /// <summary>Ключ редактора из реестра провайдера (пусто — дефолтный редактор типа)</summary>
    public string? Editor { get; init; }

    public decimal? Min { get; init; }

    public decimal? Max { get; init; }

    /// <summary>Цель пикера для Relation/File/Image (ключ реестра провайдера)</summary>
    public string? ModelName { get; init; }

    /// <summary>Варианты выбора (Select/SelectMany): в значении денормализуется <see cref="FormChoiceOption.Key"/></summary>
    public IReadOnlyCollection<FormChoiceOption> Choices { get; init; } = [];

    /// <summary>
    /// Правила поля: и от его источника (например length/unique из колонок внешней таблицы),
    /// и заданные администратором в параметрах поля (<see cref="FormFieldSettings"/>).
    /// В раскладке формы правила не хранятся — она только про представление.
    /// </summary>
    public IReadOnlyCollection<FormRuleDefinition> Rules { get; init; } = [];

    /// <summary>Значение по умолчанию в канонической кодировке (<see cref="FormValueCodec"/>)</summary>
    public JsonNode? Default { get; init; }

    /// <summary>Мешок расширения провайдера (аналог <c>Options</c> метаполя)</summary>
    public JsonNode? Options { get; init; }

    /// <summary>
    /// Тип одного элемента значения: для <see cref="FormFieldType.SelectMany"/> это
    /// <see cref="FormFieldType.Select"/>, иначе тип самого поля.
    /// </summary>
    public FormFieldType ElementType
        => Type == FormFieldType.SelectMany ? FormFieldType.Select : Type;
}

/// <summary>Вариант выбора: стабильный ключ для значения и переводимый заголовок для отображения</summary>
public record FormChoiceOption
{
    public required string Key { get; init; }

    public required string Title { get; init; }
}
