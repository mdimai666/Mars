namespace Mars.Cms.Contracts.MetaFields;

/// <summary>
/// Ключи доменных редакторов метаполей в общем реестре формы (<c>FormEditorLocator</c>): связи и
/// медиа. У них нет типизированного CLR-значения — строки владельца правит доменный редактор.
/// Значения простых типов (строка, число, дата, выбор) рисуют встроенные или общие редакторы
/// общего слоя, а выбранный администратором ключ метаполе хранит у себя в <c>Options.editor</c>.
/// Компоненты регистрирует админка (DI: <c>AddFormEditor</c>).
/// </summary>
public static class MetaFormEditors
{
    public const string Relation = "core.meta.relation";

    public const string RelationMulti = "core.meta.relation.multi";

    public const string File = "core.meta.file";

    public const string FileMulti = "core.meta.file.multi";

    /// <summary>
    /// Доменный редактор по типу и кратности (пусто — значения рисует редактор общего слоя
    /// по ключу из <c>Options.editor</c> или встроенный по типу поля).
    /// </summary>
    public static string For(MetaFieldType type, bool isMultiple) => type switch
    {
        MetaFieldType.Relation => isMultiple ? RelationMulti : Relation,
        MetaFieldType.File or MetaFieldType.Image => isMultiple ? FileMulti : File,
        _ => "",
    };
}
