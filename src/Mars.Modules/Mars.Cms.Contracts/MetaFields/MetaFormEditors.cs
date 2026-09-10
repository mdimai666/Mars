namespace Mars.Cms.Contracts.MetaFields;

/// <summary>
/// Ключи редакторов значений метаполей в общем реестре формы (<c>FormEditorLocator</c>).
/// Ключ приезжает в дескрипторе поля (<c>MetaFieldFormMapping</c>): встроенных редакторов
/// общего слоя для Relation/File/Image нет, а примитивы метаполей рисует существующий
/// инлайн-редактор вместе с кастомными редакторами из реестра <c>MetaFieldEditors</c>.
/// Компоненты регистрирует админка (<c>FormEditorLocator.Register</c>).
/// </summary>
public static class MetaFormEditors
{
    /// <summary>Примитивы по типу и кастомные редакторы метаполя (WYSIWYG, код, цвет, ссылка, дата…)</summary>
    public const string Value = "core.meta.value";

    /// <summary>То же для значения-списка (кратность или множественный выбор)</summary>
    public const string ValueMulti = "core.meta.value.multi";

    public const string Relation = "core.meta.relation";

    public const string RelationMulti = "core.meta.relation.multi";

    public const string File = "core.meta.file";

    public const string FileMulti = "core.meta.file.multi";

    /// <summary>
    /// Редактор значения метаполя по типу и кратности. Кратность важна: общий рендерер ищет
    /// редактор по (ключ, тип элемента, multiple), поэтому список значений — отдельный ключ.
    /// </summary>
    public static string For(MetaFieldType type, bool isMultiple) => type switch
    {
        MetaFieldType.Relation => isMultiple ? RelationMulti : Relation,
        MetaFieldType.File or MetaFieldType.Image => isMultiple ? FileMulti : File,
        // множественный выбор в форме всегда список значений, независимо от флага кратности
        _ => isMultiple || type == MetaFieldType.SelectMany ? ValueMulti : Value,
    };
}
