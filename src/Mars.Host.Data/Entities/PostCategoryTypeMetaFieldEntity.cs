using System.ComponentModel.DataAnnotations.Schema;

namespace Mars.Host.Data.Entities;

public class PostCategoryTypeMetaFieldEntity
{
    [ForeignKey(nameof(PostCategoryType))]
    public Guid PostCategoryTypeId { get; set; }
    public virtual PostCategoryTypeEntity? PostCategoryType { get; set; }

    [ForeignKey(nameof(MetaField))]
    public Guid MetaFieldId { get; set; }
    public virtual MetaFieldEntity? MetaField { get; set; }
}
