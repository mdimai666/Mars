using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Mappings.MetaFields;

public static class MetaFieldDetailMapping
{
    public static MetaFieldDetailResponse ToDetailResponse(this MetaFieldDto entity)
        => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Type = entity.Type,
            Description = entity.Description,
            Disabled = entity.Disabled,
            Hidden = entity.Hidden,
            IsNullable = entity.IsNullable,
            Key = entity.Key,
            MaxValue = entity.MaxValue,
            MinValue = entity.MinValue,
            ModelName = entity.ModelName,
            Order = entity.Order,
            ParentId = entity.ParentId,
            Tags = entity.Tags,
            Variants = entity.Variants?.ToResponse(),

        };

    public static MetaValueDetailResponse ToDetailResponse(this MetaValueDetailDto entity)
        => new()
        {
            Id = entity.Id,
            ParentId = entity.ParentId,
            Index = entity.Index,
            MetaField = entity.MetaField.ToDetailResponse(),

            Bool = entity.Bool,
            Int = entity.Int,
            Float = entity.Float,
            Decimal = entity.Decimal,
            Long = entity.Long,
            StringShort = entity.StringShort,
            StringText = entity.StringText,
            NULL = entity.NULL,
            DateTime = entity.DateTime,
            VariantId = entity.VariantId,
            VariantsIds = entity.VariantsIds,
            ModelId = entity.ModelId,
        };

    public static IReadOnlyCollection<MetaFieldDetailResponse> ToDetailResponse(this IReadOnlyCollection<MetaFieldDto> list)
        => list.Select(ToDetailResponse).ToList();

    public static IReadOnlyCollection<MetaValueDetailResponse> ToDetailResponse(this IReadOnlyCollection<MetaValueDetailDto> list)
        => list.Select(ToDetailResponse).ToList();

}
