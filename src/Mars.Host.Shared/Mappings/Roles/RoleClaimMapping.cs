using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Roles;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Roles;

namespace Mars.Host.Shared.Mappings.Roles;

public static class RoleClaimMapping
{
    public static RoleClaimSummaryResponse ToResponse(this RoleClaimSummary entity)
        => new()
        {
            Id = entity.Id,
            RoleId = entity.RoleId,
            ClaimType = entity.ClaimType,
            ClaimValue = entity.ClaimValue,
        };

    public static ListDataResult<RoleClaimSummaryResponse> ToResponse(this ListDataResult<RoleClaimSummary> items)
        => items.ToMap(ToResponse);

    public static PagingResult<RoleClaimSummaryResponse> ToResponse(this PagingResult<RoleClaimSummary> items)
        => items.ToMap(ToResponse);
}
