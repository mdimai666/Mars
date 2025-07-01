using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Shared.Dto.UserTypes;

/// <summary>
/// <see cref="UpdateUserTypeRequest"/>
/// </summary>
public record UpdateUserTypeQuery
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; }
}
