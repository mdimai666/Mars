using Mars.Core.Attributes;
using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Shared.Dto.PostCategories;

public record CreatePostCategoryQuery
{
    public Guid? Id { get; init; }
    public required string Title { get; init; }

    [SlugString(allowUppercase: true)]
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required Guid PostTypeId { get; init; }
    public required Guid PostCategoryTypeId { get; init; }
    public required Guid? ParentId { get; init; }

    public required bool Disabled { get; init; }
    public required IReadOnlyCollection<ModifyMetaValueDetailQuery> MetaValues { get; init; }
}
