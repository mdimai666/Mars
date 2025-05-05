using System.ComponentModel.DataAnnotations.Schema;

namespace Mars.Host.Data.Entities;

public class PostMetaValueEntity
{
    [ForeignKey(nameof(Post))]
    public Guid PostId { get; set; }
    public virtual PostEntity? Post { get; set; }

    [ForeignKey(nameof(MetaValue))]
    public Guid MetaValueId { get; set; }
    public virtual MetaValueEntity? MetaValue { get; set; }
}
