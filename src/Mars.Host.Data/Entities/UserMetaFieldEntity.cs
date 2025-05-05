using System.ComponentModel.DataAnnotations.Schema;

namespace Mars.Host.Data.Entities;

public class UserMetaFieldEntity
{
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    public virtual UserEntity? User { get; set; }

    [ForeignKey(nameof(MetaField))]
    public Guid MetaFieldId { get; set; }
    public virtual MetaFieldEntity? MetaField { get; set; }
}
