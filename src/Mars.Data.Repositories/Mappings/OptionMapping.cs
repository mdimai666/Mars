using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.Options;

namespace Mars.Host.Repositories.Mappings;

internal static class OptionMapping
{
    public static OptionSummary ToSummary(this OptionEntity entity)
        => new()
        {
            //Id = entity.Id,
            Key = entity.Key,
            //CreatedAt = entity.CreatedAt,
            //ModifiedAt = entity.ModifiedAt,
            Type = entity.Type,
            Value = entity.Value
        };

     public static OptionDetail ToDetail(this OptionEntity entity)
        => new()
        {
            Id = entity.Id,
            Key = entity.Key,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Type = entity.Type,
            Value = entity.Value
        };

    public static IReadOnlyCollection<OptionSummary> ToSummaryList(this IEnumerable<OptionEntity> entities)
        => entities.Select(ToSummary).ToArray();
}
