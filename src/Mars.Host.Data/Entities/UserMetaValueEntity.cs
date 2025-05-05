using System.ComponentModel.DataAnnotations.Schema;

namespace Mars.Host.Data.Entities;

public class UserMetaValueEntity
{
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    public virtual UserEntity? User { get; set; } = default!;

    [ForeignKey(nameof(MetaValue))]
    public Guid MetaValueId { get; set; }
    public virtual MetaValueEntity? MetaValue { get; set; } = default!;
}
