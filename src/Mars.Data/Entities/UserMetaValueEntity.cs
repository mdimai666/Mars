namespace Mars.Host.Data.Entities;

/// <summary>
/// Мета-значение пользователя (таблица <c>user_meta_values</c>).
/// </summary>
public class UserMetaValueEntity : MetaValueBase
{
    public Guid UserId { get; set; }
    public virtual UserEntity? User { get; set; }
}
