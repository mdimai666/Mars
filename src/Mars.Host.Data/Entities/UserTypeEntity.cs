using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Mars.Host.Data.Common;
using Mars.Host.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[DebuggerDisplay("{TypeName}/{Title}/{Id}")]
[EntityTypeConfiguration(typeof(UserTypeEntityConfiguration))]
public class UserTypeEntity : IBasicEntity
{
    public const string DefaultTypeName = "default";

    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }

    [Comment("Название")]
    [Required]
    public string Title { get; set; } = default!;

    [StringLength(100)]
    [Comment("Тип")]
    [Required]
    public string TypeName { get; set; } = default!;

    [Comment("Теги")]
    public List<string> Tags { get; set; } = [];

    // Relations

    public virtual ICollection<UserTypeMetaFieldEntity>? UserTypeMetaFields { get; set; }
    [NotMapped]
    public virtual List<MetaFieldEntity>? MetaFields { get; set; }

    [NotMapped] //вспомогательный, для получения
    public virtual List<UserEntity>? Users { get; set; }
}
