namespace Mars.Data.Entities;

/// <summary>
/// Мета-значение категории постов (таблица <c>post_category_meta_values</c>).
/// </summary>
public class PostCategoryMetaValueEntity : MetaValueBase
{
    public Guid PostCategoryId { get; set; }
    public virtual PostCategoryEntity? PostCategory { get; set; }
}
