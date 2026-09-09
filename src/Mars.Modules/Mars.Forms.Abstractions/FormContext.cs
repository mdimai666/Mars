namespace Mars.Forms.Abstractions;

/// <summary>Контекст запроса формы: кто владелец и для кого рендер</summary>
public sealed record FormContext
{
    /// <summary>Модель-владелец: <c>post.article</c>, <c>node.core.HttpNode</c>, <c>sql.ds1.orders</c>, <c>form.feedback</c></summary>
    public required string OwnerModel { get; init; }

    /// <summary>Ид редактируемой записи строкой (при создании — пусто)</summary>
    public string? OwnerId { get; init; }

    /// <summary>true — рендер для конечного пользователя: скрытые поля не отдаются</summary>
    public bool Client { get; init; }

    /// <summary>Язык (резерв под мультиязычность заголовков)</summary>
    public string? LangCode { get; init; }

    /// <summary>Параметры провайдера, которые не влезают в модель владельца</summary>
    public IReadOnlyDictionary<string, string?> Parameters { get; init; }
        = new Dictionary<string, string?>();

    public string? Parameter(string key)
        => Parameters.TryGetValue(key, out var value) ? value : null;
}
