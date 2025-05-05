using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Dto.MetaFields;

public static class MetaFieldRequestExtensions
{
    public static MetaFieldDto ToDto(this CreateMetaFieldRequest request)
        => new()
        {
            Id = request.Id,
            ParentId = request.ParentId,
            Title = request.Title,
            Key = request.Key,
            Type = request.Type,
            MaxValue = request.MaxValue,
            MinValue = request.MinValue,
            Description = request.Description,
            IsNullable = request.IsNullable,
            //Default = request.Default,
            Order = request.Order,
            Tags = request.Tags,
            Hidden = request.Hidden,
            Disabled = request.Disabled,
            Variants = request.Variants?.ToDto(),
            ModelName = request.ModelName,

        };

    public static MetaFieldDto ToDto(this UpdateMetaFieldRequest request)
        => new()
        {
            Id = request.Id,
            ParentId = request.ParentId,
            Title = request.Title,
            Key = request.Key,
            Type = request.Type,
            MaxValue = request.MaxValue,
            MinValue = request.MinValue,
            Description = request.Description,
            IsNullable = request.IsNullable,
            //Default = request.Default,
            Order = request.Order,
            Tags = request.Tags,
            Hidden = request.Hidden,
            Disabled = request.Disabled,
            Variants = request.Variants?.ToDto(),
            ModelName = request.ModelName,

        };

    public static IReadOnlyCollection<MetaFieldDto> ToDto(this IReadOnlyCollection<CreateMetaFieldRequest> entities)
        => entities.Select(ToDto).ToList();

    public static IReadOnlyCollection<MetaFieldDto> ToDto(this IReadOnlyCollection<UpdateMetaFieldRequest> entities)
        => entities.Select(ToDto).ToList();


    public static ModifyMetaValueDetailQuery ToQuery(this CreateMetaValueRequest request, IDictionary<Guid, MetaFieldDto> metaFields)
        => new()
        {
            Id = request.Id,
            ParentId = request.ParentId,
            Index = request.Index,
            Bool = request.Bool,
            Int = request.Int,
            Float = request.Float,
            Decimal = request.Decimal,
            Long = request.Long,
            StringShort = request.StringShort,
            StringText = request.StringText,
            VariantId = request.VariantId,
            VariantsIds = request.VariantsIds,
            DateTime = request.DateTime,
            NULL = request.NULL,
            ModelId = request.ModelId,
            MetaFieldId = request.MetaFieldId,
            MetaField = metaFields[request.MetaFieldId],
        };

    public static ModifyMetaValueDetailQuery ToQuery(this UpdateMetaValueRequest request, IDictionary<Guid, MetaFieldDto> metaFields)
        => new()
        {
            Id = request.Id,
            ParentId = request.ParentId,
            Index = request.Index,
            Bool = request.Bool,
            Int = request.Int,
            Float = request.Float,
            Decimal = request.Decimal,
            Long = request.Long,
            StringShort = request.StringShort,
            StringText = request.StringText,
            VariantId = request.VariantId,
            VariantsIds = request.VariantsIds,
            DateTime = request.DateTime,
            NULL = request.NULL,
            ModelId = request.ModelId,
            MetaFieldId = request.MetaFieldId,
            MetaField = metaFields[request.MetaFieldId],
        };

    public static IReadOnlyCollection<ModifyMetaValueDetailQuery> ToQuery(this IReadOnlyCollection<CreateMetaValueRequest> entities, IDictionary<Guid, MetaFieldDto> metaFields)
        => entities.Select(s => s.ToQuery(metaFields)).ToList();

    public static IReadOnlyCollection<ModifyMetaValueDetailQuery> ToQuery(this IReadOnlyCollection<UpdateMetaValueRequest> entities, IDictionary<Guid, MetaFieldDto> metaFields)
        => entities.Select(s => s.ToQuery(metaFields)).ToList();
}
