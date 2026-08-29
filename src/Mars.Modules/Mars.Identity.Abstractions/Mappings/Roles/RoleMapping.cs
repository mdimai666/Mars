using Mars.Contracts.Common;
using Mars.Contracts.Extensions;
using Mars.Identity.Abstractions.Dto.Roles;
using Mars.Identity.Contracts.Roles;

namespace Mars.Identity.Abstractions.Mappings.Roles;

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

    public static IReadOnlyCollection<RoleSummaryResponse> ToResponse(this IReadOnlyCollection<RoleSummary> items)
        => items.Select(ToResponse).ToArray();
}
