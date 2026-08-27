using System.ComponentModel.DataAnnotations;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Shared.Contracts.PostCategoryTypes;

public record CreatePostCategoryTypeRequest
{
    public required Guid Id { get; init; }

    [Required]
    public required string Title { get; init; }

    [StringLength(1000, MinimumLength = 3)]
    [Required]
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<CreateMetaFieldRequest> MetaFields { get; init; }
}

public record UpdatePostCategoryTypeRequest
{
    public required Guid Id { get; init; }

    [Required]
    public required string Title { get; init; }

    [StringLength(1000, MinimumLength = 3)]
    [Required]
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<UpdateMetaFieldRequest> MetaFields { get; init; }
}

public record ListPostCategoryTypeQueryRequest : BasicListQueryRequest
{
}

public record TablePostCategoryTypeQueryRequest : BasicTableQueryRequest
{
}
