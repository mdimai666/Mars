using Mars.Cms.Contracts.PostCategoryTypes;

namespace Mars.Cms.Contracts.PostCategories;

public record PostCategoryEditViewModel
{
    public required PostCategoryEditResponse PostCategory { get; init; }
    public required PostCategoryTypeDetailResponse PostCategoryType { get; init; }
}
