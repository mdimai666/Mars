namespace Mars.Forms.Contracts;

/// <summary>
/// Определение формы, которое провайдер отдаёт на рендер: дерево контейнеров
/// с дескрипторами полей и манифестом возможностей провайдера.
/// Не хранится — собирается провайдером на каждый запрос (хранится только раскладка,
/// см. <see cref="FormLayoutSettings"/>).
/// </summary>
public record FormDefinition
{
    /// <summary>Модель-владелец формы: <c>post.article</c>, <c>node.core.HttpNode</c>, <c>sql.ds1.orders</c>, <c>form.feedback</c></summary>
    public required string OwnerModel { get; init; }

    /// <summary>Нормализованное дерево: все доступные поля ровно один раз</summary>
    public IReadOnlyCollection<FormItem> Items { get; init; } = [];

    public FormProviderManifest? Manifest { get; init; }

    /// <summary>Зоны в порядке объявления (для рендера и дизайнера)</summary>
    public IReadOnlyCollection<string> Zones
        => Manifest is { Zones.Count: > 0 }
            ? Manifest.Zones.Select(z => z.Key).ToList()
            : Items.Select(i => i.Zone ?? "").Distinct().ToList();

    /// <summary>Все листы-поля дерева (обход в глубину, порядок дерева)</summary>
    public IEnumerable<FormItem> Fields() => Items.FlattenFields();
}

/// <summary>
/// Хранимая раскладка формы: только порядок, зоны и настройки элементов, без дескрипторов.
/// Для поста — <c>post_types.Options["form"]</c>, для автономных форм — таблица <c>forms</c>.
/// </summary>
public record FormLayoutSettings
{
    public IReadOnlyCollection<FormItem> Items { get; init; } = [];
}
