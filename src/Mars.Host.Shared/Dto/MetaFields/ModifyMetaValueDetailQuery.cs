using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Dto.MetaFields;

public record ModifyMetaValueDetailQuery : ModifyMetaValueDetailDto
{
    public required MetaFieldDto MetaField { get; init; }

    public static ModifyMetaValueDetailQuery GetBlank(MetaFieldDto metaField, Guid? id = null)
    {
        return new()
        {
            Id = id ?? Guid.NewGuid(),
            Index = 0,

            Bool = null,
            Int = null,
            Float = null,
            Decimal = null,
            Long = null,
            DateTime = null,
            ModelId = null,
            StringShort = metaField.Type == MetaFieldType.String ? "" : null,
            StringText = metaField.Type == MetaFieldType.Text ? "" : null,
            VariantId = null,
            VariantsIds = [],

            MetaField = metaField,
            MetaFieldId = metaField.Id,
        };
    }

    public object? GetValueSimple() => base.GetValueSimple(MetaField.Type);
}
