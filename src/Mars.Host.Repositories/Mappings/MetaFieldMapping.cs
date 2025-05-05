using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Repositories.Mappings;

internal static class MetaFieldMapping
{
    public static MetaFieldDto ToDto(this MetaFieldEntity entity)
        => new()
        {
            Id = entity.Id,
            ParentId = entity.ParentId,
            Key = entity.Key,
            Title = entity.Title,
            Type = (MetaFieldType)entity.Type,

            Description = entity.Description,
            MaxValue = entity.MaxValue,
            MinValue = entity.MinValue,
            ModelName = entity.ModelName,
            Order = entity.Order,
            Tags = entity.Tags,

            //Default = entity.Default?.ToDto(),
            Disabled = entity.Disabled,
            Hidden = entity.Hidden,
            IsNullable = entity.IsNullable,

            Variants = entity.Variants.ToDto(),
        };

    public static IReadOnlyCollection<MetaFieldDto> ToDto(this List<MetaFieldEntity> entities)
        => entities.Select(ToDto).ToList();

    public static MetaFieldEntity ToEntity(this MetaFieldDto dto)
        => new()
        {
            Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            ParentId = dto.ParentId,
            Title = dto.Title,
            Key = dto.Key,
            Type = dto.Type.ToEntity(),
            MaxValue = dto.MaxValue,
            MinValue = dto.MinValue,
            Description = dto.Description,
            IsNullable = dto.IsNullable,
            //Default = dto.Default,
            Order = dto.Order,
            Tags = dto.Tags.ToList(),
            Hidden = dto.Hidden,
            Disabled = dto.Disabled,
            Variants = dto.Variants?.ToEntity() ?? [],
            ModelName = dto.ModelName,
        };

    public static List<MetaFieldEntity> ToEntity(this IReadOnlyCollection<MetaFieldDto> entities)
        => entities.Select(ToEntity).ToList();

    public static EMetaFieldType ToEntity(this MetaFieldType dto)
        => (EMetaFieldType)(int)dto;
}
