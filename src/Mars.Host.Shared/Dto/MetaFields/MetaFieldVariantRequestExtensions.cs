using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Dto.MetaFields;

public static class MetaFieldVariantRequestExtensions
{
    public static MetaFieldVariantDto ToDto(this CreateMetaFieldVariantRequest request)
       => new()
       {
           Id = request.Id,
           Title = request.Title,
           Tags = request.Tags,
           Value = request.Value,
           Disable = request.Disable,
       };

    public static MetaFieldVariantDto ToDto(this UpdateMetaFieldVariantRequest request)
       => new()
       {
           Id = request.Id,
           Title = request.Title,
           Tags = request.Tags,
           Value = request.Value,
           Disable = request.Disable,
       };

    public static IReadOnlyCollection<MetaFieldVariantDto> ToDto(this IReadOnlyCollection<CreateMetaFieldVariantRequest> entities)
        => entities.Select(ToDto).ToList();

    public static IReadOnlyCollection<MetaFieldVariantDto> ToDto(this IReadOnlyCollection<UpdateMetaFieldVariantRequest> entities)
        => entities.Select(ToDto).ToList();
}

