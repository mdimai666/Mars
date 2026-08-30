using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Mars.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Data.Entities;

[DebuggerDisplay("{TypeName}/{Title}/{Id}")]
public class PostCategoryTypeEntity : IBasicEntity //Пост ли? Можно упростить для всех
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

    public virtual ICollection<MetaFieldEntity>? MetaFields { get; set; }

    [NotMapped]
    public virtual List<PostCategoryEntity>? PostCategories { get; set; }
}
