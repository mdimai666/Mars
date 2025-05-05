using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Roles;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Roles;

namespace Mars.Host.Shared.Mappings.Roles;

public static class RoleMapping
{
    public static RoleSummaryResponse ToResponse(this RoleSummary entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Name = entity.Name,
        };

    public static RoleDetailResponse ToResponse(this RoleDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Name = entity.Name,
        };

    public static ListDataResult<RoleSummaryResponse> ToResponse(this ListDataResult<RoleSummary> items)
        => items.ToMap(ToResponse);

    public static PagingResult<RoleSummaryResponse> ToResponse(this PagingResult<RoleSummary> items)
        => items.ToMap(ToResponse);
}
