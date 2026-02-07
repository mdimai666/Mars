using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Shared.Dto.PostCategoryTypes;

public record PostCategoryTypeDetail : PostCategoryTypeSummary
{
    public required DateTimeOffset? ModifiedAt { get; init; }

    public required IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; }
}
