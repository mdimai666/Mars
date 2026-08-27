using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Mars.Host.Data.Common;
using Mars.Host.Data.OwnedTypes.Feedbacks;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[Comment("Обратная связь")]
public class FeedbackEntity : IBasicEntity
{
    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }

    [Comment("Название")]
    public string Title { get; set; } = default!;

    [Comment("Текст")]
    public string? Content { get; set; }

    [Comment("Телефон")]
    public string? Phone { get; set; }

    [Comment("Заполненное имя")]
    public string? FilledUsername { get; set; }

    [Comment("Email")]
    [EmailAddress]
    public string? Email { get; set; }

    [Comment("Тип обратной связи")]
    public EFeedbackType FeedbackType { get; set; } = EFeedbackType.InfoMessage;

    // Relations

    [ForeignKey(nameof(AuthorizedUser))]
    public Guid? AuthorizedUserId { get; set; }
    public virtual UserEntity? AuthorizedUser { get; set; }

}
