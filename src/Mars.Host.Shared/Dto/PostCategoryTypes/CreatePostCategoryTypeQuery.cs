using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.PostCategoryTypes;

namespace Mars.Host.Shared.Dto.PostCategoryTypes;

/// <summary>
/// <see cref="CreatePostCategoryTypeRequest"/>
/// </summary>
public record CreatePostCategoryTypeQuery
{
    public Guid? Id { get; init; }
    public required string Title { get; init; }
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; }

}
