using Mars.Cms.Contracts.MetaFields;

namespace Mars.Cms.Abstractions.Dto.MetaFields;

public static class MetaValueRequestExtensions
{
    public static ModifyMetaValueDetailDto ToDto(this CreateMetaValueRequest request)
        => new()
        {
            Id = request.Id,
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
            ModelId = request.ModelId,
            MetaFieldId = request.MetaFieldId,
        };

    public static ModifyMetaValueDetailDto ToDto(this UpdateMetaValueRequest request)
        => new()
        {
            Id = request.Id,
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
            ModelId = request.ModelId,
            MetaFieldId = request.MetaFieldId,
        };

    public static IReadOnlyCollection<ModifyMetaValueDetailDto> ToDto(this IReadOnlyCollection<CreateMetaValueRequest> entities)
        => entities.Select(ToDto).ToList();

    public static IReadOnlyCollection<ModifyMetaValueDetailDto> ToDto(this IReadOnlyCollection<UpdateMetaValueRequest> entities)
        => entities.Select(ToDto).ToList();

    public static MetaValueRelationModelsListQuery ToQuery(this MetaValueRelationModelsListQueryRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,
            ModelName = request.ModelName
        };

    public static ModifyMetaValueDetailQuery ToModifyDto(this MetaValueDetailDto request)
        => new()
        {
            Id = request.Id,
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
            ModelId = request.ModelId,
            MetaFieldId = request.MetaField.Id,
            MetaField = request.MetaField,
        };
}
