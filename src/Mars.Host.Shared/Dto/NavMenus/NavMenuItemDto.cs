namespace Mars.Host.Shared.Dto.NavMenus;

public class NavMenuItemDto
{
    public required Guid Id { get; init; } = Guid.NewGuid();
    public required Guid ParentId { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required string? Icon { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public required bool RolesInverse { get; init; }
    public required string Class { get; init; }
    public required string Style { get; init; }
    public required bool OpenInNewTab { get; init; }
    public required bool Disabled { get; init; }
    public required bool IsHeader { get; init; }
    public required bool IsDivider { get; init; }

    //public IEnumerable<NavMenuItem> GetItems(NavMenuEntity nav)
    //{

    //    return nav.MenuItems.Where(x => x.ParentId == Id);
    //}

    //public static NavMenuItem Copy(NavMenuItem item)
    //{
    //    return new NavMenuItem
    //    {
    //        Id = Guid.NewGuid(),
    //        Title = item.Title,
    //        Url = item.Url,
    //        Class = item.Class,
    //        Disabled = item.Disabled,
    //        Style = item.Style,
    //        OpenInNewTab = item.OpenInNewTab,
    //        Icon = item.Icon,
    //        ParentId = item.ParentId,
    //        Roles = item.Roles,
    //        RolesInverse = item.RolesInverse,
    //    };
    //}

    //[NotMapped]
    //public bool IsFontIcon => !(Icon?.Contains('/') ?? true);
}
