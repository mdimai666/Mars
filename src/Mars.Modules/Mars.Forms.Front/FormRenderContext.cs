using Mars.Forms.Contracts;
using Microsoft.AspNetCore.Components;

namespace Mars.Forms.Front;

/// <summary>Контекст рендера формы: каскадируется в контейнеры и редакторы</summary>
public sealed record FormRenderContext
{
    public required FormDefinition Definition { get; init; }

    public required IFormValueStore Values { get; init; }

    /// <summary>true — рендер для конечного пользователя</summary>
    public bool Client { get; init; }

    /// <summary>Резолвер переводимых заголовков по <see cref="FormFieldDescriptor.TitleKey"/> (локализатор потребителя)</summary>
    public Func<string, string>? TitleResolver { get; init; }

    /// <summary>Свой рендер листа-поля вместо встроенного <c>FormFieldRow</c> — точка, где провайдер
    /// подмешивает доменные компоненты (например, существующие редакторы мета-значений поста).</summary>
    public RenderFragment<FormItem>? FieldTemplate { get; init; }

    /// <summary>
    /// Хуки отложенной записи: редакторы, которые держат значение у себя до сохранения,
    /// регистрируются в общем на форму списке. Один экземпляр на все зоны.
    /// </summary>
    public FormCommitHooks Commits { get; init; } = new();

    /// <summary>
    /// Живые редакторы полей: по ключу поля доступно значение редактора, который держит его у себя
    /// (запись извне и сохранение формы из редактора кода). Один экземпляр на все зоны.
    /// </summary>
    public FormLiveEditors LiveEditors { get; init; } = new();
}
