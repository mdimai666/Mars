using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Shared.Dto.PostCategories;

public class PostCategoryDetail : PostCategorySummary
{
    public required bool Disabled { get; init; }

    public required IReadOnlyCollection<MetaValueDto> MetaValues { get; init; }
}
