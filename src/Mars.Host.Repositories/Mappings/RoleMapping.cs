using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.Roles;

namespace Mars.Host.Repositories.Mappings;

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
