using Mars.Forms.Contracts;

namespace Mars.Cms.Contracts.MetaFields;

/// <summary>
/// Тип метаполя → тип поля общей формы. Один источник для сервера (дескриптор поля в
/// <c>MetaFieldFormMapping</c>) и клиента (определение поля в общем редакторе).
/// </summary>
public static class MetaFieldTypeFormMapping
{
    public static FormFieldType ToFormFieldType(this MetaFieldType type) => type switch
    {
        MetaFieldType.String => FormFieldType.String,
        MetaFieldType.Text => FormFieldType.Text,
        MetaFieldType.Bool => FormFieldType.Bool,
        MetaFieldType.Int => FormFieldType.Int,
        MetaFieldType.Long => FormFieldType.Long,
        MetaFieldType.Float => FormFieldType.Float,
        MetaFieldType.Decimal => FormFieldType.Decimal,
        MetaFieldType.DateTime => FormFieldType.DateTime,
        MetaFieldType.Select => FormFieldType.Select,
        MetaFieldType.SelectMany => FormFieldType.SelectMany,
        MetaFieldType.Relation => FormFieldType.Relation,
        MetaFieldType.File => FormFieldType.File,
        MetaFieldType.Image => FormFieldType.Image,
        // вычислимое поле: значение не передаётся, резолвится на чтении
        MetaFieldType.Query => FormFieldType.Computed,
        _ => FormFieldType.Object,
    };
}
