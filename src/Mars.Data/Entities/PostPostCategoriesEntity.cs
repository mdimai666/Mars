using System.ComponentModel.DataAnnotations.Schema;

namespace Mars.Host.Data.Entities;

public class PostPostCategoriesEntity
{
    [ForeignKey(nameof(Post))]
    public Guid PostId { get; set; }
    public virtual PostEntity? Post { get; set; }

    [ForeignKey(nameof(PostCategory))]
    public Guid PostCategoryId { get; set; }
    public virtual PostCategoryEntity? PostCategory { get; set; }
}
