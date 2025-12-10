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
            ParentId = Guid.Empty,

            Bool = false,
            Int = 0,
            Float = 0,
            Decimal = 0,
            Long = 0,
            DateTime = DateTime.MinValue,
            ModelId = Guid.Empty,
            NULL = false,
            StringShort = metaField.Type == MetaFieldType.String ? "" : null,
            StringText = metaField.Type == MetaFieldType.Text ? "" : null,
            VariantId = Guid.Empty,
            VariantsIds = [],

            MetaField = metaField,
            MetaFieldId = metaField.Id,
        };
    }

    public object? GetValueSimple() => base.GetValueSimple(MetaField.Type);
}
