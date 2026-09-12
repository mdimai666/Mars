using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Contracts;

namespace Mars.Cms.Contracts.Posts;

public record PostEditViewModel
{
    public required PostEditResponse Post { get; init; }
    public required PostTypeDetailResponse PostType { get; init; }

    /// <summary>
    /// Определение формы редактирования: поля и узлы сетки с дескрипторами
    /// (системные слоты и метаполя типа). См. <c>ai/FormEngineGuide.md</c>.
    /// </summary>
    public required FormDefinition Form { get; init; }
}
