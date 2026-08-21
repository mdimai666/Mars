using Mars.Host.Data.OwnedTypes.MetaFields;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Utils;

namespace Mars.Host.Repositories.Mappings;

internal static class MetaFieldVariantMapping
{
    public static MetaFieldVariant ToEntity(this MetaFieldVariantDto dto)
        => new()
        {
            Id = dto.Id,
            Key = MetaFieldKeyNormalizer.Normalize(dto.Key),
            Title = dto.Title,
            Tags = dto.Tags.ToList(),
            Value = dto.Value,
            Disable = dto.Disable,
        };

    public static List<MetaFieldVariant> ToEntity(this IReadOnlyCollection<MetaFieldVariantDto> list)
        => list.Select(ToEntity).ToList();

}
