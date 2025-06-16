using System.ComponentModel.DataAnnotations;
using Mars.Host.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.OwnedTypes.PostTypes;

/// <summary>
/// Non table (owned jsonb)
/// </summary>
public class PostStatusEntity : IBasicEntity
{
    //[Key] is jsonb
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

    [Comment("Теги")]
    public List<string> Tags { get; set; } = [];

    public static List<PostStatusEntity> DefaultStatuses()
    {
        return new List<PostStatusEntity>
        {
            new PostStatusEntity
            {
                Id = Guid.NewGuid(),
                Title = "Черновик",
                Slug = "draft",
                CreatedAt = DateTime.Now,
            },
            new PostStatusEntity
            {
                Id = Guid.NewGuid(),
                Title = "На проверке",
                Slug = "pending",
                CreatedAt = DateTime.Now,
            },
            new PostStatusEntity
            {
                Id = Guid.NewGuid(),
                Title = "Опубликовано",
                Slug = "publish",
                CreatedAt = DateTime.Now,
            },
            new PostStatusEntity
            {
                Id = Guid.NewGuid(),
                Title = "Скрыто",
                Slug = "hidden",
                CreatedAt = DateTime.Now,
            },
            new PostStatusEntity
            {
                Id = Guid.NewGuid(),
                Title = "Удалено",
                Slug = "trash",
                CreatedAt = DateTime.Now,
            },
        };
    }
}
