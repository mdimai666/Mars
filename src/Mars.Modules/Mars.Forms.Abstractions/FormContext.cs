namespace Mars.Forms.Abstractions;

/// <summary>Контекст запроса формы: кто владелец и для кого рендер</summary>
public sealed record FormContext
{
    /// <summary>Модель-владелец: <c>post.article</c>, <c>node.core.HttpNode</c></summary>
    public required string OwnerModel { get; init; }

    /// <summary>true — рендер для конечного пользователя: скрытые поля не отдаются</summary>
    public bool Client { get; init; }
}
