using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.MetaFields;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Repositories.Mappings;

internal static class MetaValueMapping
{
    public static MetaValueDto ToDto(this MetaValueEntity entity)
        => new()
        {
            Id = entity.Id,
            ModelId = entity.ModelId,
            Type = (MetaFieldType)entity.MetaField.Type,
            Index = entity.Index,
            //Value = entity.Get(),
            Value = ConvertObjectValue(entity),
            VariantId = entity.VariantId,
            VariantsIds = entity.VariantsIds,
            MetaField = entity.MetaField!.ToDto(),
        };

    internal static object? ConvertObjectValue(MetaValueEntity metaValue)
    {
        if (metaValue.MetaField.Type == EMetaFieldType.Select)
        {
            return metaValue.MetaField.Variants.FirstOrDefault(s => s.Id == metaValue.VariantId)?.ToDto();
        }
        else if (metaValue.MetaField.Type == EMetaFieldType.SelectMany)
        {
            return metaValue.MetaField.Variants.Where(s => metaValue.VariantsIds.Contains(s.Id)).Select(s => s.ToDto()).ToArray();
        }
        else
        {
            return metaValue.Get();
        }
    }

    public static IReadOnlyCollection<MetaValueDto> ToDto(this List<MetaValueEntity> entities)
        => entities.Select(ToDto).ToList();

    public static MetaFieldVariantDto ToDto(this MetaFieldVariant entity)
        => new()
        {
            Id = entity.Id,
            Tags = entity.Tags,
            Title = entity.Title,
            Value = entity.Value,
            Disable = entity.Disable
        };

    public static IReadOnlyCollection<MetaFieldVariantDto> ToDto(this List<MetaFieldVariant> entities)
        => entities.Select(ToDto).ToList();

    public static MetaFieldVariantValueDto ToValueDto(this MetaFieldVariant entity)
        => new()
        {
            Id = entity.Id,
            Tags = entity.Tags,
            Title = entity.Title,
            Value = entity.Value,
        };

    public static MetaValueDetailDto ToDetailDto(this MetaValueEntity entity)
        => new()
        {
            Id = entity.Id,
            ParentId = entity.ParentId,
            Index = entity.Index,
            //Value = entity.Get(),
            Bool = entity.Bool,
            Int = entity.Int,
            Float = entity.Float,
            Decimal = entity.Decimal,
            Long = entity.Long,
            DateTime = entity.DateTime,
            NULL = entity.NULL,
            StringShort = entity.StringShort,
            StringText = entity.StringText,

            VariantId = entity.VariantId,
            VariantsIds = entity.VariantsIds,
            MetaField = entity.MetaField!.ToDto(),
            ModelId = entity.ModelId,
        };

    public static IReadOnlyCollection<MetaValueDetailDto> ToDetailDto(this List<MetaValueEntity> entities)
        => entities.Select(ToDetailDto).ToList();

    public static MetaValueEntity ToEntity(this ModifyMetaValueDetailQuery dto)
        => new()
        {
            Id = dto.Id,
            ParentId = dto.ParentId,
            Index = dto.Index,
            Type = (EMetaFieldType)dto.MetaField.Type,

            Bool = dto.Bool,
            Int = dto.Int,
            Float = dto.Float,
            Decimal = dto.Decimal,
            Long = dto.Long,
            StringText = dto.StringText,
            StringShort = dto.StringShort,
            NULL = dto.NULL,
            DateTime = dto.DateTime,
            VariantId = dto.VariantId,
            VariantsIds = dto.VariantsIds,
            ModelId = dto.ModelId,

            MetaFieldId = dto.MetaFieldId,
        };

    public static List<MetaValueEntity> ToEntity(this IReadOnlyCollection<ModifyMetaValueDetailQuery> entities)
        => entities.Select(ToEntity).ToList();

}
