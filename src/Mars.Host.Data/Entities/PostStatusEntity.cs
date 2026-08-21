using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Mars.Host.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

/// <summary>
/// Статусы постов типа (системная таблица); порядок/цвет = схема канбан-доски
/// </summary>
[DebuggerDisplay("PostStatus/{Slug}/{Id}/{Title}")]
public class PostStatusEntity : IBasicEntity
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
    [Comment("Значение")]
    public string Slug { get; set; } = default!;

    [Comment("Цвет (канбан)")]
    public string Color { get; set; } = "";

    [Comment("Порядок (канбан)")]
    public int Order { get; set; }

    // Relations

    public Guid PostTypeId { get; set; }
    public virtual PostTypeEntity? PostType { get; set; }

    public static List<PostStatusEntity> DefaultStatuses()
    {
        return new List<PostStatusEntity>
        {
            new PostStatusEntity
            {
                Id = Guid.NewGuid(),
                Title = "Черновик",
                Slug = "draft",
                Order = 0,
                CreatedAt = DateTime.Now,
            },
            new PostStatusEntity
            {
                Id = Guid.NewGuid(),
                Title = "На проверке",
                Slug = "pending",
                Order = 1,
                CreatedAt = DateTime.Now,
            },
            new PostStatusEntity
            {
                Id = Guid.NewGuid(),
                Title = "Опубликовано",
                Slug = "publish",
                Order = 2,
                CreatedAt = DateTime.Now,
            },
            new PostStatusEntity
            {
                Id = Guid.NewGuid(),
                Title = "Скрыто",
                Slug = "hidden",
                Order = 3,
                CreatedAt = DateTime.Now,
            },
            new PostStatusEntity
            {
                Id = Guid.NewGuid(),
                Title = "Удалено",
                Slug = "trash",
                Order = 4,
                CreatedAt = DateTime.Now,
            },
        };
    }
}
