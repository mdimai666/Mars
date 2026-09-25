namespace Mars.Forms.Contracts;

/// <summary>
/// Определение формы, которое провайдер отдаёт на рендер: плоский упорядоченный список элементов
/// с дескрипторами полей и зонами размещения. Не хранится — собирается провайдером на каждый
/// запрос (хранится только раскладка, см. <see cref="FormLayoutSettings"/>).
/// </summary>
public record FormDefinition
{
    /// <summary>Модель-владелец формы: <c>post.article</c>, <c>node.core.HttpNode</c>, <c>sql.ds1.orders</c>, <c>form.feedback</c></summary>
    public required string OwnerModel { get; init; }

    /// <summary>Зоны в порядке объявления (для рендера и дизайнера)</summary>
    public IReadOnlyCollection<FormZoneDescriptor> Zones { get; init; } = [];

    /// <summary>Элементы в порядке отображения: поля и узлы сетки (контейнеры, ряды, колонки,
    /// заголовки, разделители)</summary>
    public IReadOnlyCollection<FormItem> Items { get; init; } = [];

    /// <summary>Поля в порядке раскладки (структурные и неполевые узлы пропускаются)</summary>
    public IEnumerable<FormItem> Fields() => Items.FlattenFields();
}

/// <summary>
/// Хранимая раскладка формы: порядок, зоны, видимость, ширина и узлы сетки — без дескрипторов.
/// Для поста — <c>post_types.Options["form"]</c>, для автономных форм — таблица <c>forms</c>.
/// </summary>
public record FormLayoutSettings
{
    public IReadOnlyCollection<FormItem> Items { get; init; } = [];
}
