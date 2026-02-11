using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Shared.Dto.PostCategories;

public class PostCategoryDetail : PostCategorySummary
{
    public required string Type { get; init; }
    public required string PostType { get; init; }
    public required bool Disabled { get; init; }

    public required IReadOnlyCollection<MetaValueDto> MetaValues { get; init; }
}
