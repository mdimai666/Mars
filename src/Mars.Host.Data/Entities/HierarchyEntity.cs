using System.ComponentModel.DataAnnotations;
using Mars.Host.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

public abstract class HierarchyEntity : IBasicEntity
{
    [Key]
    [Comment("ИД")]
    public virtual Guid Id { get; set; }

    [Comment("Создан")]
    public virtual DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public virtual DateTimeOffset? ModifiedAt { get; set; }

    [Required]
    [Comment("slug")]
    public virtual string Slug { get; set; } = string.Empty;

    [Comment("Теги")]
    public List<string> Tags { get; set; } = [];

    [Required]
    [Comment("Название")]
    public virtual string Title { get; set; } = string.Empty;

    /// <summary>
    /// /{rootId}/{parentId}/{id}/
    /// </summary>
    [Required]
    public virtual string Path { get; set; } = default!;

    /// <summary>
    /// /news/tech/ai
    /// </summary>
    [Required]
    public virtual string SlugPath { get; set; } = default!;

    [Required]
    public virtual Guid RootId { get; set; }

    public virtual Guid? ParentId { get; set; }

    [Required]
    public virtual Guid[] PathIds { get; set; } = [];

    public virtual int LevelsCount { get; set; }

    [Comment("Отключен")]
    public virtual bool Disabled { get; set; }

}
