using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.NavMenus;
using Mars.Host.Shared.Dto.NavMenus;

namespace Mars.Host.Repositories.Mappings;

internal static class NavMenuMapping
{
    public static NavMenuSummary ToSummary(this NavMenuEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            Slug = entity.Slug,
            Disabled = entity.Disabled,
            Tags = entity.Tags,
        };

    public static NavMenuDetail ToDetail(this NavMenuEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Title = entity.Title,
            Slug = entity.Slug,
            Disabled = entity.Disabled,
            Tags = entity.Tags,
            MenuItems = entity.MenuItems.Select(ToDto).ToList(),
            Class = entity.Class,
            Style = entity.Style,
            Roles = entity.Roles,
            RolesInverse = entity.RolesInverse,
        };

    public static NavMenuItemDto ToDto(this NavMenuItem entity)
        => new()
        {
            Id = entity.Id,
            ParentId = entity.ParentId,
            Title = entity.Title,
            Url = entity.Url,
            Icon = entity.Icon,
            Roles = entity.Roles,
            RolesInverse = entity.RolesInverse,
            Class = entity.Class,
            Style = entity.Style,
            OpenInNewTab = entity.OpenInNewTab,
            Disabled = entity.Disabled,
            IsHeader = entity.IsHeader,
            IsDivider = entity.IsDivider,
        };

    public static IReadOnlyCollection<NavMenuSummary> ToSummaryList(this IEnumerable<NavMenuEntity> entities)
        => entities.Select(ToSummary).ToArray();
    public static IReadOnlyCollection<NavMenuDetail> ToDetailList(this IEnumerable<NavMenuEntity> entities)
        => entities.Select(ToDetail).ToArray();
}
