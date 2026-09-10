using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Contracts.PostCategoryTypes;

namespace Mars.Cms.Abstractions.Dto.PostCategoryTypes;

/// <summary>
/// <see cref="CreatePostCategoryTypeRequest"/>
/// </summary>
public record CreatePostCategoryTypeQuery : IGeneralMetaFieldsSupportDto
{
    public Guid? Id { get; init; }
    public required string Title { get; init; }
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; }

}
