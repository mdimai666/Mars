using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Mars.Host.Data.Entities;
using Microsoft.EntityFrameworkCore;
using PluginExample.Data.Configurations;

namespace PluginExample.Data.Entities;

[DebuggerDisplay("PluginNewsEntity/{Title}/{Id}")]
[EntityTypeConfiguration(typeof(PluginNewsEntityConfiguration))]
public class PluginNewsEntity
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

    [Comment("Текст")]
    public string? Content { get; set; }

    [Comment("ИД пользователя")]
    public Guid UserId { get; set; }
    public virtual UserEntity? User { get; set; }


    // Relations
    //public virtual ICollection<PluginNewsFilesEntity>? PostFiles { get; set; }
    //[Comment("Файлы")]
    //[NotMapped]
    //public virtual List<FileEntity>? Files { get; set; }
}
