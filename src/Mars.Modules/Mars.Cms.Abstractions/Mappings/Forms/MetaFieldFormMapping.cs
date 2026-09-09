using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Contracts.MetaFields;
using Mars.Forms.Contracts;

namespace Mars.Cms.Abstractions.Mappings.Forms;

/// <summary>
/// Метаполе → дескриптор поля формы. Метаполе остаётся полем данных (значение в EAV)
/// и только производит дескриптор для общего слоя форм; правила и редактор при этом живут
/// на определении поля, а не в раскладке формы.
/// </summary>
public static class MetaFieldFormMapping
{
    public static FormFieldDescriptor ToFormFieldDescriptor(this MetaFieldDto field) => new()
    {
        Key = field.Key,
        Title = field.Title,
        Type = field.Type.ToFormFieldType(),
        Required = !field.IsNullable,
        Multiple = field.IsMultiple,
        Description = field.Description,
        Editor = field.Options.GetEditor(),
        Min = field.MinValue,
        Max = field.MaxValue,
        ModelName = field.ModelName,
        Choices = field.Variants?.Select(v => new FormChoiceOption { Key = v.Key, Title = v.Title }).ToList() ?? [],
        Options = field.Options?.DeepClone(),
        SettingsOnForm = false,
    };

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
