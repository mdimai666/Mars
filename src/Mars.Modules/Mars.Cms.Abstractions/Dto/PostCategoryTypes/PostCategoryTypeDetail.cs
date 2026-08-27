using Mars.Cms.Abstractions.Dto.MetaFields;

namespace Mars.Cms.Abstractions.Dto.PostCategoryTypes;

public record PostCategoryTypeDetail : PostCategoryTypeSummary
{
    public required DateTimeOffset? ModifiedAt { get; init; }

    public required IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; }
}
