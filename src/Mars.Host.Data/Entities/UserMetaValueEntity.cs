using System.ComponentModel.DataAnnotations.Schema;

namespace Mars.Host.Data.Entities;

/// <summary>
/// Мета-значение пользователя (таблица <c>user_meta_values</c>).
/// </summary>
public class UserMetaValueEntity : MetaValueBase
{
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    public virtual UserEntity? User { get; set; }
}
