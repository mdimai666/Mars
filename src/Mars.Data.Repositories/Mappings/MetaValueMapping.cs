using Mars.Data.Entities;
using Mars.Data.OwnedTypes.MetaFields;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Contracts.MetaFields;

namespace Mars.Data.Repositories.Mappings;

internal static class MetaValueMapping
{
    public static MetaValueDto ToDto(this MetaValueBase entity)
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

    internal static object? ConvertObjectValue(MetaValueBase metaValue)
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

    public static IReadOnlyCollection<MetaValueDto> ToDto(this IEnumerable<MetaValueBase> entities)
        => entities.Select(ToDto).ToList();

    public static IReadOnlyDictionary<string, MetaValueDto> ToDictionaryDto(this IEnumerable<MetaValueBase> entities)
        => entities.GroupBy(s => s.MetaField.Key)
                   .ToDictionary(g => g.Key, g =>
                   {
                       // мульти-значения (несколько строк на поле) не теряются в словаре
                       var rows = g.OrderBy(s => s.Index).Select(ToDto).ToList();
                       return rows.Count == 1 ? rows[0] : rows[0] with { MultiValues = rows };
                   });

    public static MetaFieldVariantDto ToDto(this MetaFieldVariant entity)
        => new()
        {
            Id = entity.Id,
            Key = entity.Key,
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
            Key = entity.Key,
            Tags = entity.Tags,
            Title = entity.Title,
            Value = entity.Value,
        };

    public static MetaValueDetailDto ToDetailDto(this MetaValueBase entity)
        => new()
        {
            Id = entity.Id,
            Index = entity.Index,
            //Value = entity.Get(),
            Bool = entity.Bool,
            Int = entity.Int,
            Float = entity.Float,
            Decimal = entity.Decimal,
            Long = entity.Long,
            DateTime = entity.DateTime,
            StringShort = entity.StringShort,
            StringText = entity.StringText,

            VariantId = entity.VariantId,
            VariantsIds = entity.VariantsIds,
            MetaField = entity.MetaField!.ToDto(),
            ModelId = entity.ModelId,
        };

    public static IReadOnlyCollection<MetaValueDetailDto> ToDetailDto(this IEnumerable<MetaValueBase> entities)
        => entities.Select(ToDetailDto).ToList();

    public static IReadOnlyDictionary<string, MetaValueDetailDto> ToDictionaryDetailDto(this IEnumerable<MetaValueBase> entities)
        => entities.GroupBy(s => s.MetaField.Key)
                   .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Index).First().ToDetailDto());

    public static TValue ToEntity<TValue>(this ModifyMetaValueDetailQuery dto) where TValue : MetaValueBase, new()
        => new()
        {
            Id = dto.Id,
            Index = dto.Index,
            Type = (EMetaFieldType)dto.MetaField.Type,

            Bool = dto.Bool,
            Int = dto.Int,
            Float = dto.Float,
            Decimal = dto.Decimal,
            Long = dto.Long,
            StringText = dto.StringText,
            StringShort = dto.StringShort,
            DateTime = dto.DateTime,
            VariantId = dto.VariantId,
            VariantsIds = dto.VariantsIds,
            ModelId = dto.ModelId,

            MetaFieldId = dto.MetaFieldId,
        };

    public static List<TValue> ToEntity<TValue>(this IReadOnlyCollection<ModifyMetaValueDetailQuery> entities) where TValue : MetaValueBase, new()
        => entities.Select(ToEntity<TValue>).ToList();

}
