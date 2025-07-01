using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Mappings.MetaFields;

public static class MetaFieldMapping
{
    public static MetaFieldResponse ToResponse(this MetaFieldDto entity)
        => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Type = entity.Type,
            Key = entity.Key,
        };

    public static MetaValueResponse ToResponse(this MetaValueDto entity)
        => new()
        {
            Id = entity.Id,
            MetaField = entity.MetaField.ToResponse(),
            Value = entity.Value,
        };

    public static MetaFieldVariantResponse ToResponse(this MetaFieldVariantDto entity)
        => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Value = entity.Value,
            Tags = entity.Tags,
            Disable = entity.Disable,
        };

    public static MetaFieldVariantValueDto ToValueDto(this MetaFieldVariantDto entity)
        => new()
        {
            Id = entity.Id,
            Tags = entity.Tags,
            Title = entity.Title,
            Value = entity.Value,
        };

    public static MetaFieldVariantValueResponse ToResponse(this MetaFieldVariantValueDto entity)
        => new()
        {
            Id = entity.Id,
            Tags = entity.Tags,
            Title = entity.Title,
            Value = entity.Value,
        };

    public static MetaValueResponse ToResponse(this MetaValueDetailDto entity)
        => new()
        {
            Id = entity.Id,
            MetaField = entity.MetaField.ToResponse(),
            Value = entity.GetValueSimple(entity.MetaField.Type),
        };

    public static IReadOnlyCollection<MetaFieldResponse> ToResponse(this IReadOnlyCollection<MetaFieldDto> list)
        => list.Select(ToResponse).ToList();

    public static IReadOnlyCollection<MetaValueResponse> ToResponse(this IReadOnlyCollection<MetaValueDto> list)
        => list.Select(ToResponse).ToList();

    public static IReadOnlyCollection<MetaFieldVariantResponse> ToResponse(this IReadOnlyCollection<MetaFieldVariantDto> list)
        => list.Select(ToResponse).ToList();

    public static IReadOnlyCollection<MetaFieldVariantValueResponse> ToResponse(this IReadOnlyCollection<MetaFieldVariantValueDto> list)
        => list.Select(ToResponse).ToList();

    public static IReadOnlyCollection<MetaValueResponse> ToResponse(this IReadOnlyCollection<MetaValueDetailDto> list)
        => list.Select(ToResponse).ToList();

}
