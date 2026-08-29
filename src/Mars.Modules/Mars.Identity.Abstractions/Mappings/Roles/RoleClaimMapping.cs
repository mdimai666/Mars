using Mars.Contracts.Common;
using Mars.Contracts.Extensions;
using Mars.Identity.Abstractions.Dto.Roles;
using Mars.Identity.Contracts.Roles;

namespace Mars.Identity.Abstractions.Mappings.Roles;

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
