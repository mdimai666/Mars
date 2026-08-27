using Mars.Cms.Abstractions.Dto.MetaFields;

namespace Mars.Identity.Abstractions.Dto.UserTypes;

public record UserTypeDetail : UserTypeSummary
{
    public required DateTimeOffset? ModifiedAt { get; init; }

    public required IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; }
}
