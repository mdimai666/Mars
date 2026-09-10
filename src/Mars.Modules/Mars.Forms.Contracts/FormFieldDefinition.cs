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

    /// <summary>
    /// Модель источника (например <c>MetaFieldEditModel</c>): доменные панели настроек типа
    /// правят её напрямую, общий редактор — только общие параметры определения.
    /// </summary>
    public object? Source { get; set; }

    /// <summary>Заголовок типа для показа (источник знает свои названия); пусто — имя <see cref="Type"/></summary>
    public string? TypeTitle { get; set; }

    /// <summary>Значок типа для заголовка строки</summary>
    public string? TypeIcon { get; set; }

    /// <summary>Поле защищено владельцем: нельзя удалить и сменить тип (feature-поле поста)</summary>
    public bool Protected { get; set; }

    /// <summary>Ключ поля зафиксирован владельцем (поле контента поста)</summary>
    public bool KeyLocked { get; set; }

    /// <summary>Тип поля поддерживает ограничитель min/max</summary>
    public bool SupportsLimits { get; set; }

    /// <summary>Тип поля поддерживает несколько значений</summary>
    public bool SupportsMultiple { get; set; }

    /// <summary>Редакторы значения, которые источник предлагает для поля; пусто — реестр фронта</summary>
    public IReadOnlyCollection<(string Key, string Title)> Editors { get; set; } = [];

    /// <summary>Доступные правила валидации с заголовками; пусто — каталог общего слоя</summary>
    public IReadOnlyCollection<(string Key, string Title)> RuleOptions { get; set; } = [];

    /// <summary>
    /// Параметры правил по типу правила (что показывать в редакторе); для правил, которых здесь нет,
    /// действует общий каталог <c>FormRuleParams</c>. Источник задаёт свой набор, когда его валидатор
    /// понимает не все общие параметры (например, правило длины метаполя не принимает сообщение).
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? RuleParams { get; set; }
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

    public bool CanEditRequired { get; init; }

    public bool CanEditMultiple { get; init; }

    public bool CanHide { get; init; }

    public bool CanDisable { get; init; }

    public bool CanEditTags { get; init; }

    public bool CanEditOrder { get; init; }

    public bool CanEditLimits { get; init; }

    public bool CanEditEditor { get; init; }

    public bool CanEditRules { get; init; }

    /// <summary>Системные слоты: правятся только правила и редактор, остальное задано каталогом</summary>
    public static FormDefinitionCapabilities SystemFields { get; } = new()
    {
        CanEditEditor = true,
        CanEditRules = true,
    };

    /// <summary>Поля, которые источник хранит у себя (метаполя): полный набор действий</summary>
    public static FormDefinitionCapabilities MetaFields { get; } = new()
    {
        CanAdd = true,
        CanClone = true,
        CanDelete = true,
        CanChangeType = true,
        CanEditKey = true,
        CanEditTitle = true,
        CanEditDescription = true,
        CanEditRequired = true,
        CanEditMultiple = true,
        CanHide = true,
        CanDisable = true,
        CanEditTags = true,
        CanEditOrder = true,
        CanEditLimits = true,
        CanEditEditor = true,
        CanEditRules = true,
    };
}
