using Mars.Data.Entities;
using Mars.Identity.Abstractions.Dto.Roles;

namespace Mars.Data.Repositories.Mappings;

internal static class RoleMapping
{
    public static RoleSummary ToSummary(this RoleEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Name = entity.Name!,
        };

    public static RoleDetail ToDetail(this RoleEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Name = entity.Name!,
        };

    public static IReadOnlyCollection<RoleSummary> ToSummaryList(this IEnumerable<RoleEntity> entities)
        => entities.Select(ToSummary).ToArray();
}
