using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Identity.Contracts.UserTypes;

namespace Mars.Identity.Abstractions.Dto.UserTypes;

/// <summary>
/// <see cref="CreateUserTypeRequest"/>
/// </summary>
public record CreateUserTypeQuery : IGeneralMetaFieldsSupportDto
{
    public Guid? Id { get; init; }
    public required string Title { get; init; }
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; }

}
