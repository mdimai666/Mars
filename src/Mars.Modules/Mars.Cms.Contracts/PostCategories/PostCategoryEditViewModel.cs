using Mars.Cms.Contracts.PostCategoryTypes;
using Mars.Forms.Contracts;

namespace Mars.Cms.Contracts.PostCategories;

public record PostCategoryEditViewModel
{
    public required PostCategoryEditResponse PostCategory { get; init; }
    public required PostCategoryTypeDetailResponse PostCategoryType { get; init; }

    /// <summary>Дерево формы метаполей категории (общий слой Mars.Forms)</summary>
    public FormDefinition? Form { get; init; }
}
