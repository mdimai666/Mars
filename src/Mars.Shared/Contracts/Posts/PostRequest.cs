using System.ComponentModel.DataAnnotations;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Shared.Contracts.Posts;

public record CreatePostRequest
{
    public required Guid? Id { get; init; }

    [Required]
    public required string Title { get; init; }

    [StringLength(PostTypeConstants.TypeNameMaxLength, MinimumLength = PostTypeConstants.TypeNameMinLength)]
    [Required]
    public required string Type { get; init; }

    [Required]
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required string? Content { get; init; }
    public required string? Status { get; init; }
    public required string? Excerpt { get; init; }
    public required string LangCode { get; init; }

    public required IReadOnlyCollection<CreateMetaValueRequest> MetaValues { get; init; }

}

public record UpdatePostRequest
{
    public required Guid Id { get; init; }

    [Required]
    public required string Title { get; init; }

    [StringLength(PostTypeConstants.TypeNameMaxLength, MinimumLength = PostTypeConstants.TypeNameMinLength)]
    [Required]
    public required string Type { get; init; }

    [Required]
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required string? Content { get; init; }
    public required string? Status { get; init; }

    public required string? Excerpt { get; init; }
    public required string LangCode { get; init; }

    public required IReadOnlyCollection<UpdateMetaValueRequest> MetaValues { get; init; }

}

public record ListPostQueryRequest : BasicListQueryRequest
{

}

public record TablePostQueryRequest : BasicTableQueryRequest
{
}
