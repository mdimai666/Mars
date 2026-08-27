using System.ComponentModel.DataAnnotations;
using Mars.Host.Data.Common;
using Mars.Host.Data.OwnedTypes.NavMenus;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

public class NavMenuEntity : IBasicEntity
{
    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }

    [Required]
    [Comment("Название")]
    public string Title { get; set; } = default!;

    [Required]
    [Comment("slug")]
    public virtual string Slug { get; set; } = Guid.NewGuid().ToString();

    [Comment("Элементы")]
    public List<NavMenuItem> MenuItems { get; set; } = [];

    [Comment("Class")]
    public string Class { get; set; } = "";
    [Comment("Style")]
    public string Style { get; set; } = "";

    [Comment("Роли")]
    public List<string> Roles { get; set; } = [];

    //[NotMapped]
    //[JsonIgnore]
    //[Comment("Роли")]
    //public IEnumerable<string> SetRoles { get => Roles; set => Roles = value.ToList(); }

    /// <summary>
    /// Не для ролей
    /// </summary>
    [Comment("Не для ролей")]
    public bool RolesInverse { get; set; }

    [Comment("Отключен")]
    public bool Disabled { get; set; }

    [Comment("Теги")]
    public List<string> Tags { get; set; } = [];
}
