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
        Editor = EditorKey(field),
        Min = field.MinValue,
        Max = field.MaxValue,
        ModelName = field.ModelName,
        Choices = field.Variants?.Select(v => new FormChoiceOption { Key = v.Key, Title = v.Title }).ToList() ?? [],
        Options = field.Options?.DeepClone(),
    };

    /// <summary>
    /// Ключ редактора значения: у связей и медиа — доменный (типизированного значения нет),
    /// у остальных типов — выбранный администратором. Пусто — встроенный редактор общего слоя.
    /// </summary>
    static string? EditorKey(MetaFieldDto field)
    {
        var key = field.Type is MetaFieldType.Relation or MetaFieldType.File or MetaFieldType.Image
            ? MetaFormEditors.For(field.Type, field.IsMultiple)
            : field.Options.GetEditor();

        return key.Length > 0 ? key : null;
    }
}
