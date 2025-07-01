using System.ComponentModel.DataAnnotations.Schema;

namespace Mars.Host.Data.Entities;

public class UserTypeMetaFieldEntity
{
    [ForeignKey(nameof(UserType))]
    public Guid UserTypeId { get; set; }
    public virtual UserTypeEntity? UserType { get; set; }

    [ForeignKey(nameof(MetaField))]
    public Guid MetaFieldId { get; set; }
    public virtual MetaFieldEntity? MetaField { get; set; }
}
