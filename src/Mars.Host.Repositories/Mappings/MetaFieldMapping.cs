using Mars.Core.Extensions;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Utils;
using Mars.Shared.Contracts.MetaFields;
using DefaultValueContract = Mars.Shared.Contracts.MetaFields.MetaFieldDefaultValue;
using DefaultValueOwned = Mars.Host.Data.OwnedTypes.MetaFields.MetaFieldDefaultValue;

namespace Mars.Host.Repositories.Mappings;

internal static class MetaFieldMapping
{
    public static MetaFieldDto ToDto(this MetaFieldEntity entity)
        => new()
        {
            Id = entity.Id,
            Key = entity.Key,
            Title = entity.Title,
            Type = (MetaFieldType)entity.Type,

            Description = entity.Description,
            MaxValue = entity.MaxValue,
            MinValue = entity.MinValue,
            ModelName = entity.ModelName,
            Order = entity.Order,
            Tags = entity.Tags,

            Default = entity.Default?.ToDto(),
            Options = entity.Options,
            Disabled = entity.Disabled,
            Hidden = entity.Hidden,
            IsNullable = entity.IsNullable,

            Variants = entity.Variants.ToDto(),
        };

    public static IReadOnlyCollection<MetaFieldDto> ToDto(this IEnumerable<MetaFieldEntity> entities)
        => entities.Select(ToDto).ToList();

    public static MetaFieldEntity ToEntity(this MetaFieldDto dto)
        => new()
        {
            Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            Title = dto.Title,
            Key = MetaFieldKeyNormalizer.Normalize(dto.Key),
            Type = dto.Type.ToEntity(),
            MaxValue = dto.MaxValue,
            MinValue = dto.MinValue,
            Description = dto.Description,
            IsNullable = dto.IsNullable,
            Default = dto.Default?.ToOwned(),
            Options = dto.Options,
            Order = dto.Order,
            Tags = dto.Tags.ToList(),
            Hidden = dto.Hidden,
            Disabled = dto.Disabled,
            Variants = dto.Variants?.ToEntity() ?? [],
            ModelName = dto.ModelName.AsNullIfEmpty(),
        };

    public static List<MetaFieldEntity> ToEntity(this IReadOnlyCollection<MetaFieldDto> entities)
        => entities.Select(ToEntity).ToList();

    public static EMetaFieldType ToEntity(this MetaFieldType dto)
        => (EMetaFieldType)(int)dto;

    public static DefaultValueContract ToDto(this DefaultValueOwned value)
        => new()
        {
            Bool = value.Bool,
            Int = value.Int,
            Float = value.Float,
            Decimal = value.Decimal,
            Long = value.Long,
            StringText = value.StringText,
            StringShort = value.StringShort,
            DateTime = value.DateTime,
            VariantId = value.VariantId,
            VariantsIds = value.VariantsIds,
            ModelId = value.ModelId,
        };

    public static DefaultValueOwned ToOwned(this DefaultValueContract value)
        => new()
        {
            Bool = value.Bool,
            Int = value.Int,
            Float = value.Float,
            Decimal = value.Decimal,
            Long = value.Long,
            StringText = value.StringText,
            StringShort = value.StringShort,
            DateTime = value.DateTime,
            VariantId = value.VariantId,
            VariantsIds = value.VariantsIds,
            ModelId = value.ModelId,
        };
}
