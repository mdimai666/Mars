using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Shared.Dto.UserTypes;

public record UserTypeDetail : UserTypeSummary
{
    public required DateTimeOffset? ModifiedAt { get; init; }

    public required IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; }
}
