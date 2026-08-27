namespace Mars.Host.Data.Entities;

/// <summary>
/// Мета-значение поста (таблица <c>post_meta_values</c>).
/// </summary>
public class PostMetaValueEntity : MetaValueBase
{
    public Guid PostId { get; set; }
    public virtual PostEntity? Post { get; set; }
}
