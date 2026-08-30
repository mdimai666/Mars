using Mars.Cms.Abstractions.Dto.MetaFields;

namespace Mars.Identity.Abstractions.Dto.UserTypes;

/// <summary>
/// <see cref="UpdateUserTypeRequest"/>
/// </summary>
public record UpdateUserTypeQuery : IGeneralMetaFieldsSupportDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; }
}
