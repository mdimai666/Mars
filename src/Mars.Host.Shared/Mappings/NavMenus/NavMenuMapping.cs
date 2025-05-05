using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.NavMenus;
using Mars.Shared.Common;
using Mars.Shared.Contracts.NavMenus;

namespace Mars.Host.Shared.Mappings.NavMenus;

public static class NavMenuMapping
{
    public static NavMenuSummaryResponse ToResponse(this NavMenuSummary entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            Slug = entity.Slug,
            Tags = entity.Tags,
            Disabled = entity.Disabled,
        };

    public static NavMenuDetailResponse ToResponse(this NavMenuDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Title = entity.Title,
            Slug = entity.Slug,
            Tags = entity.Tags,
            Disabled = entity.Disabled,

            Class = entity.Class,
            Style = entity.Style,
            Roles = entity.Roles,
            RolesInverse = entity.RolesInverse,
            MenuItems = entity.MenuItems.Select(ToResponse).ToList(),
        };

    public static NavMenuItemResponse ToResponse(this NavMenuItemDto entity)
        => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Url = entity.Url,
            Disabled = entity.Disabled,

            Class = entity.Class,
            Style = entity.Style,
            Roles = entity.Roles,
            Icon = entity.Icon,
            IsDivider = entity.IsDivider,
            IsHeader = entity.IsHeader,
            OpenInNewTab = entity.OpenInNewTab,
            ParentId = entity.ParentId,
            RolesInverse    = entity.RolesInverse,
        };

    public static ListDataResult<NavMenuSummaryResponse> ToResponse(this ListDataResult<NavMenuSummary> items)
        => items.ToMap(ToResponse);

    public static PagingResult<NavMenuSummaryResponse> ToResponse(this PagingResult<NavMenuSummary> items)
        => items.ToMap(ToResponse);
}
