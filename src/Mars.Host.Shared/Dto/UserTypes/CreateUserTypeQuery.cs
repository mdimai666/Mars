using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.UserTypes;

namespace Mars.Host.Shared.Dto.UserTypes;

/// <summary>
/// <see cref="CreateUserTypeRequest"/>
/// </summary>
public record CreateUserTypeQuery
{
    public Guid? Id { get; init; }
    public required string Title { get; init; }
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; }

}
