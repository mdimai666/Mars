using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Dto.MetaFields;

public record ModifyMetaValueDetailQuery : ModifyMetaValueDetailDto
{
    public required MetaFieldDto MetaField { get; init; }

    public static ModifyMetaValueDetailQuery GetBlank(MetaFieldDto metaField, Guid? id = null)
    {
        var def = metaField.Default;
        return new()
        {
            Id = id ?? Guid.NewGuid(),
            Index = 0,

            Bool = def?.Bool,
            Int = def?.Int,
            Float = def?.Float,
            Decimal = def?.Decimal,
            Long = def?.Long,
            DateTime = def?.DateTime,
            ModelId = def?.ModelId,
            StringShort = metaField.Type == MetaFieldType.String ? def?.StringShort ?? ""
                        : metaField.Type == MetaFieldType.Select ? metaField.GetDefaultVariantKey()
                        : def?.StringShort,
            StringText = metaField.Type == MetaFieldType.Text ? def?.StringText ?? "" : def?.StringText,
            VariantId = def?.VariantId,
            VariantsIds = def?.VariantsIds ?? [],

            MetaField = metaField,
            MetaFieldId = metaField.Id,
        };
    }

    public object? GetValueSimple() => base.GetValueSimple(MetaField.Type);
}
