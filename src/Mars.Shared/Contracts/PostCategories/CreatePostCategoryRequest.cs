using System.ComponentModel.DataAnnotations;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Shared.Contracts.PostCategories;

public record CreatePostCategoryRequest
{
    public required Guid? Id { get; init; }

    [Required]
    public required string Title { get; init; }

    [StringLength(PostCategoryTypeConstants.TypeNameMaxLength, MinimumLength = PostCategoryTypeConstants.TypeNameMinLength)]
    [Required]
    public required string Type { get; init; }

    [StringLength(PostCategoryTypeConstants.TypeNameMaxLength, MinimumLength = PostCategoryTypeConstants.TypeNameMinLength)]
    [Required]
    public required string PostType { get; init; }

    [Required]
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required Guid? ParentId { get; init; }

    public required bool Disabled { get; init; }
    public required IReadOnlyCollection<CreateMetaValueRequest> MetaValues { get; init; }

}

public record UpdatePostCategoryRequest
{
    public required Guid Id { get; init; }

    [Required]
    public required string Title { get; init; }

    [StringLength(PostCategoryTypeConstants.TypeNameMaxLength, MinimumLength = PostCategoryTypeConstants.TypeNameMinLength)]
    [Required]
    public required string Type { get; init; }

    [StringLength(PostCategoryTypeConstants.TypeNameMaxLength, MinimumLength = PostCategoryTypeConstants.TypeNameMinLength)]
    [Required]
    public required string PostType { get; init; }

    [Required]
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required Guid? ParentId { get; init; }

    public required bool Disabled { get; init; }
    public required IReadOnlyCollection<UpdateMetaValueRequest> MetaValues { get; init; }

}

public record ListPostCategoryQueryRequest : BasicListQueryRequest
{

}

public record TablePostCategoryQueryRequest : BasicTableQueryRequest
{
}
