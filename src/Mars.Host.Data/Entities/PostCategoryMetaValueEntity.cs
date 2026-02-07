using System.ComponentModel.DataAnnotations.Schema;

namespace Mars.Host.Data.Entities;

public class PostCategoryMetaValueEntity
{
    [ForeignKey(nameof(PostCategory))]
    public Guid PostCategoryId { get; set; }
    public virtual PostCategoryEntity? PostCategory { get; set; }

    [ForeignKey(nameof(MetaValue))]
    public Guid MetaValueId { get; set; }
    public virtual MetaValueEntity? MetaValue { get; set; }
}
