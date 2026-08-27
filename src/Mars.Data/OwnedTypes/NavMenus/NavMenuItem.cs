using System.ComponentModel.DataAnnotations.Schema;
using Mars.Host.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.OwnedTypes.NavMenus;

/// <summary>
/// [jsonb] owned
/// </summary>
public class NavMenuItem
{
    [Comment("#")]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Comment("Родитель")]
    public Guid ParentId { get; set; }

    [Comment("Название")]
    public string Title { get; set; } = "";

    [Comment("Ссылка")]
    public string Url { get; set; } = "";

    [Comment("Иконка")]
    public string? Icon { get; set; }

    [Comment("Роли")]
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Не для ролей
    /// </summary>
    [Comment("Не для ролей")]
    public bool RolesInverse { get; set; }

    [Comment("Class")]
    public string Class { get; set; } = "";

    [Comment("Style")]
    public string Style { get; set; } = "";

    [Comment("Открывать в новой вкладке")]
    public bool OpenInNewTab { get; set; }

    [Comment("Отключен")]
    public bool Disabled { get; set; }

    [Comment("Заголовок")]
    public bool IsHeader { get; set; }

    [Comment("Разделитель")]
    public bool IsDivider { get; set; }

    public IEnumerable<NavMenuItem> GetItems(NavMenuEntity nav)
    {

        return nav.MenuItems.Where(x => x.ParentId == Id);
    }

    public static NavMenuItem Copy(NavMenuItem item)
    {
        return new NavMenuItem
        {
            Id = Guid.NewGuid(),
            Title = item.Title,
            Url = item.Url,
            Class = item.Class,
            Disabled = item.Disabled,
            Style = item.Style,
            OpenInNewTab = item.OpenInNewTab,
            Icon = item.Icon,
            ParentId = item.ParentId,
            Roles = item.Roles,
            RolesInverse = item.RolesInverse,
        };
    }

    [NotMapped]
    public bool IsFontIcon => !(Icon?.Contains('/') ?? true);
}
