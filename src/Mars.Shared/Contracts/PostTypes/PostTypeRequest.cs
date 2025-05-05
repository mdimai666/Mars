using System.ComponentModel.DataAnnotations;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Common;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Shared.Contracts.PostTypes;

public record CreatePostTypeRequest
{
    public required Guid Id { get; init; }

    [Required]
    public required string Title { get; init; }

    [StringLength(1000, MinimumLength = 3)]
    [Required]
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<CreatePostStatusRequest> PostStatusList { get; init; }
    public required IReadOnlyCollection<string> EnabledFeatures { get; init; }
    public required bool Disabled { get; init; }
    public required CreatePostContentSettingsRequest PostContentSettings { get; init; }
    public required IReadOnlyCollection<CreateMetaFieldRequest> MetaFields { get; init; }
}

public record UpdatePostTypeRequest
{
    public required Guid Id { get; init; }

    [Required]
    public required string Title { get; init; }

    [StringLength(1000, MinimumLength = 3)]
    [Required]
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<UpdatePostStatusRequest> PostStatusList { get; init; }
    public required IReadOnlyCollection<string> EnabledFeatures { get; init; }
    public required bool Disabled { get; init; }
    public required UpdatePostContentSettingsRequest PostContentSettings { get; init; }
    public required IReadOnlyCollection<UpdateMetaFieldRequest> MetaFields { get; init; }
}

public record CreatePostStatusRequest
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
}

public record CreatePostContentSettingsRequest
{
    /// <summary>
    /// <see cref="PostTypeConstants.DefaultPostContentTypes.PlainText"/>
    /// </summary>
    public required string PostContentType { get; init; }
    public required string? CodeLang { get; init; }
}

public record UpdatePostStatusRequest
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
}

public record UpdatePostContentSettingsRequest
{
    /// <summary>
    /// <see cref="PostTypeConstants.DefaultPostContentTypes.PlainText"/>
    /// </summary>
    public required string PostContentType { get; init; }
    public required string? CodeLang { get; init; }
}


public record ListPostTypeQueryRequest : BasicListQueryRequest
{
}

public record TablePostTypeQueryRequest : BasicTableQueryRequest
{
}
