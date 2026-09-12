using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Contracts;

namespace Mars.Cms.Contracts.Posts;

public record PostEditViewModel
{
    public required PostEditResponse Post { get; init; }
    public required PostTypeDetailResponse PostType { get; init; }

    /// <summary>
    /// Определение формы редактирования: дерево контейнеров с дескрипторами полей
    /// (системные слоты и метаполя типа). См. <c>ai/FormEnginePlan.md</c>.
    /// </summary>
    public required FormDefinition Form { get; init; }
}
