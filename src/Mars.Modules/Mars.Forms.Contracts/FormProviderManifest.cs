namespace Mars.Forms.Contracts;

/// <summary>
/// Манифест провайдера формы: что провайдер умеет. Дизайнер формы показывает только
/// объявленное здесь, поэтому наборы валидаторов, редакторов и видов контейнеров
/// у разных провайдеров свои.
/// </summary>
public record FormProviderManifest
{
    public required string OwnerModel { get; init; }

    public string? Title { get; init; }

    /// <summary>Зоны размещения (у поста main/publish/extra, у нод и виджетов — одна)</summary>
    public IReadOnlyCollection<FormZoneDescriptor> Zones { get; init; } = [];

    /// <summary>Поддерживаемые виды контейнеров (встроенные — <see cref="FormItemKinds.All"/>)</summary>
    public IReadOnlyCollection<string> ContainerKinds { get; init; } = FormItemKinds.All;

    /// <summary>Доступные правила валидации (глобальные ∩ свои провайдера)</summary>
    public IReadOnlyCollection<string> RuleTypes { get; init; } = [];

    /// <summary>Доступные ключи редакторов</summary>
    public IReadOnlyCollection<string> EditorKeys { get; init; } = [];

    public FormProviderCapabilities Capabilities { get; init; } = new();
}

/// <summary>Зона размещения элементов формы</summary>
public record FormZoneDescriptor
{
    public required string Key { get; init; }

    public required string Title { get; init; }
}

/// <summary>Что разрешено делать с формой в дизайнере</summary>
public record FormProviderCapabilities
{
    public bool CanReorder { get; init; } = true;

    public bool CanAddSections { get; init; } = true;

    public bool CanHide { get; init; } = true;

    /// <summary>Можно назначать правила полям (только при <see cref="FormFieldDescriptor.SettingsOnForm"/>)</summary>
    public bool CanEditRules { get; init; } = true;

    /// <summary>Можно добавлять поля, которых нет у провайдера (автономные формы)</summary>
    public bool CanAddFields { get; init; }
}
