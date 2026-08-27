using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.Roles;

namespace Mars.Host.Repositories.Mappings;

internal static class RoleClaimMapping
{
    public static RoleClaimSummary ToSummary(this RoleClaimEntity entity)
        => new()
        {
            Id = entity.Id,
            RoleId = entity.RoleId,
            ClaimType = entity.ClaimType!,
            ClaimValue = entity.ClaimValue!
        };

    public static IReadOnlyCollection<RoleClaimSummary> ToSummaryList(this IEnumerable<RoleClaimEntity> entities)
        => entities.Select(ToSummary).ToArray();
}
