using Mars.Host.Shared.Extensions;
using Mars.Shared.Contracts.NavMenus;

namespace Mars.Host.Shared.Dto.NavMenus;

public static class NavMenuRequestExtensions
{
    public static CreateNavMenuQuery ToQuery(this CreateNavMenuRequest request)
        => new()
        {
            Id = request.Id,
            Title = request.Title,
            Slug = request.Slug,
            Disabled = request.Disabled,
            Tags = request.Tags,
            MenuItems = request.MenuItems.Select(ToQuery).ToList(),
            Class = request.Class,
            Style = request.Style,
            Roles = request.Roles,
            RolesInverse = request.RolesInverse,

        };
    public static UpdateNavMenuQuery ToQuery(this UpdateNavMenuRequest request)
        => new()
        {
            Id = request.Id,
            Title = request.Title,
            Slug = request.Slug,
            Disabled = request.Disabled,
            Tags = request.Tags,
            MenuItems = request.MenuItems.Select(ToQuery).ToList(),
            Class = request.Class,
            Style = request.Style,
            Roles = request.Roles,
            RolesInverse = request.RolesInverse,
        };

    public static ListNavMenuQuery ToQuery(this ListNavMenuQueryRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,
        };

    public static ListNavMenuQuery ToQuery(this TableNavMenuQueryRequest request)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,
        };

    public static NavMenuItemDto ToQuery(this CreateNavMenuItemRequest request)
        => new()
        {
            Id = request.Id,
            ParentId = request.ParentId,
            Title = request.Title,
            Url = request.Url,
            Icon = request.Icon,
            Roles = request.Roles,
            RolesInverse = request.RolesInverse,
            Class = request.Class,
            Style = request.Style,
            OpenInNewTab = request.OpenInNewTab,
            Disabled = request.Disabled,
            IsHeader = request.IsHeader,
            IsDivider = request.IsDivider,
        };

    public static NavMenuItemDto ToQuery(this UpdateNavMenuItemRequest request)
        => new()
        {
            Id = request.Id,
            ParentId = request.ParentId,
            Title = request.Title,
            Url = request.Url,
            Icon = request.Icon,
            Roles = request.Roles,
            RolesInverse = request.RolesInverse,
            Class = request.Class,
            Style = request.Style,
            OpenInNewTab = request.OpenInNewTab,
            Disabled = request.Disabled,
            IsHeader = request.IsHeader,
            IsDivider = request.IsDivider,
        };
}
