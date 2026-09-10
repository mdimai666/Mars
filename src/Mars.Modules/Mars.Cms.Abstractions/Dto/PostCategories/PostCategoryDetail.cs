using Mars.Cms.Abstractions.Dto.MetaFields;

namespace Mars.Cms.Abstractions.Dto.PostCategories;

public class PostCategoryDetail : PostCategorySummary
{
    public required string Type { get; init; }
    public required string PostType { get; init; }
    public required bool Disabled { get; init; }

    public required IReadOnlyDictionary<string, MetaValueDto> MetaValues { get; init; }
}
