using System.Text.Json.Nodes;

namespace Mars.Forms.Contracts;

/// <summary>
/// Редактируемое определение поля — то, что правит общий редактор определений
/// (<c>FieldDefinitionsEditor</c>). Источник строит их из своего хранилища (метаполя —
/// <c>meta_fields</c>, системные слоты — каталог + параметры типа) и забирает правки обратно.
/// Рендер формы по-прежнему идёт по <see cref="FormFieldDescriptor"/>: определение — про настройку,
/// дескриптор — про отрисовку.
/// </summary>
public class FormFieldDefinition
{
    public Guid Id { get; set; }

    public string Key { get; set; } = "";

    public string Title { get; set; } = "";

    /// <summary>Ключ ресурса переводимого заголовка (системные слоты); пустой — заголовок литеральный</summary>
    public string? TitleKey { get; set; }

    public string? Description { get; set; }

    public FormFieldType Type { get; set; }

    public bool Required { get; set; }

    public bool ReadOnly { get; set; }

    /// <summary>Поле допускает несколько значений</summary>
    public bool Multiple { get; set; }

    public bool Hidden { get; set; }

    public bool Disabled { get; set; }

    /// <summary>Ключ редактора; пустой — редактор по умолчанию для типа</summary>
    public string? Editor { get; set; }

    public decimal? Min { get; set; }

    public decimal? Max { get; set; }

    /// <summary>Цель пикера для Relation/File/Image</summary>
    public string? ModelName { get; set; }

    public IReadOnlyCollection<string> Tags { get; set; } = [];

    public int Order { get; set; }

    /// <summary>Зона формы по умолчанию; размещение правится в дизайнере раскладки</summary>
    public string? Zone { get; set; }

    /// <summary>Фича владельца, которая включает поле (null — доступно всегда)</summary>
    public string? Feature { get; set; }

    /// <summary>Мешок доменных параметров источника (аналог <c>Options</c> метаполя)</summary>
    public JsonNode? Options { get; set; }

    public List<FormRuleDefinition> Rules { get; set; } = [];
}

/// <summary>
/// Что редактор определений разрешает делать с полями источника: у системных слотов состав задан
/// каталогом и фичами типа (добавлять/дублировать/удалять нечего), у метаполей — полный набор.
/// </summary>
public record FormDefinitionCapabilities
{
    public bool CanAdd { get; init; }

    public bool CanClone { get; init; }

    public bool CanDelete { get; init; }

    public bool CanChangeType { get; init; }

    public bool CanEditKey { get; init; }

    public bool CanEditTitle { get; init; }

    public bool CanEditDescription { get; init; }

    public bool CanEditEditor { get; init; }

    public bool CanEditRules { get; init; }

    /// <summary>Системные слоты: правятся только правила и редактор, остальное задано каталогом</summary>
    public static FormDefinitionCapabilities SystemFields { get; } = new()
    {
        CanEditEditor = true,
        CanEditRules = true,
    };
}
