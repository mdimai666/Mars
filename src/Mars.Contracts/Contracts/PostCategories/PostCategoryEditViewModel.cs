using Mars.Contracts.PostCategoryTypes;

namespace Mars.Contracts.PostCategories;

public record PostCategoryEditViewModel
{
    public required PostCategoryEditResponse PostCategory { get; init; }
    public required PostCategoryTypeDetailResponse PostCategoryType { get; init; }
}
