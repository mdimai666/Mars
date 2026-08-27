using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Mars.Shared.Common;

namespace Mars.Shared.Contracts.Galleries;

public record CreateGalleryRequest
{
    public required Guid? Id { get; init; }

    [Required]
    public required string Title { get; init; }

    [StringLength(1000, MinimumLength = 3)]
    [Required]
    public required string Type { get; init; }

    [Required]
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
}

public record UpdateGalleryRequest
{
    public required Guid Id { get; init; }

    [Required]
    public required string Title { get; init; }

    [StringLength(1000, MinimumLength = 3)]
    [Required]
    public required string Type { get; init; }

    [Required]
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
}

public record ListGalleryQueryRequest : BasicListQueryRequest
{
}

public record TableGalleryQueryRequest : BasicTableQueryRequest
{
}
