using System.ComponentModel.DataAnnotations.Schema;

namespace Mars.Host.Data.Entities;

/// <summary>
/// Мета-значение категории постов (таблица <c>post_category_meta_values</c>).
/// </summary>
public class PostCategoryMetaValueEntity : MetaValueBase
{
    [ForeignKey(nameof(PostCategory))]
    public Guid PostCategoryId { get; set; }
    public virtual PostCategoryEntity? PostCategory { get; set; }
}
