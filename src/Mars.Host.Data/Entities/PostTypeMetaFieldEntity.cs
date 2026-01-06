using System.ComponentModel.DataAnnotations.Schema;

namespace Mars.Host.Data.Entities;

public class PostTypeMetaFieldEntity
{
    [ForeignKey(nameof(PostType))]
    public Guid PostTypeId { get; set; }
    public virtual PostTypeEntity? PostType { get; set; }

    [ForeignKey(nameof(MetaField))]
    public Guid MetaFieldId { get; set; }
    public virtual MetaFieldEntity? MetaField { get; set; }
}
