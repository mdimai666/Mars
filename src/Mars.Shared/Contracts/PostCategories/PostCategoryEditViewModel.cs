using Mars.Shared.Contracts.PostCategoryTypes;

namespace Mars.Shared.Contracts.PostCategories;

public record PostCategoryEditViewModel
{
    public required PostCategoryEditResponse PostCategory { get; init; }
    public required PostCategoryTypeDetailResponse PostCategoryType { get; init; }
}
