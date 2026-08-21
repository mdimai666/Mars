using System.ComponentModel.DataAnnotations.Schema;

namespace Mars.Host.Data.Entities;

/// <summary>
/// Мета-значение поста (таблица <c>post_meta_values</c>).
/// </summary>
public class PostMetaValueEntity : MetaValueBase
{
    [ForeignKey(nameof(Post))]
    public Guid PostId { get; set; }
    public virtual PostEntity? Post { get; set; }
}
