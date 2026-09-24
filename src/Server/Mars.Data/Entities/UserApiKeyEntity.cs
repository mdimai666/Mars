using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Mars.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Data.Entities;

[Comment("API ключ пользователя")]
public class UserApiKeyEntity : IBasicUserEntity
{
    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }

    [Comment("Название")]
    public string Name { get; set; } = default!;

    [Comment("Хэш секретной части ключа (SHA-256, base64)")]
    public string KeyHash { get; set; } = default!;

    [Comment("Префикс ключа для отображения")]
    public string KeyPrefix { get; set; } = default!;

    [Comment("Действителен до")]
    public DateTimeOffset? ExpiresAt { get; set; }

    // Relations

    public Guid UserId { get; set; }
    public virtual UserEntity? User { get; set; }
}
