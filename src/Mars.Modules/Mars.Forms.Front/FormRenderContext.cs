using Mars.Forms.Contracts;

namespace Mars.Forms.Front;

/// <summary>Контекст рендера формы: каскадируется в контейнеры и редакторы</summary>
public sealed record FormRenderContext
{
    public required FormDefinition Definition { get; init; }

    public required FormValuesModel Values { get; init; }

    /// <summary>true — рендер для конечного пользователя</summary>
    public bool Client { get; init; }
}
