using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Shared.Contracts.PostJsons;

public record CreatePostJsonRequest
{
    public Guid? Id { get; init; }

    [Required]
    public string Title { get; init; } = default!;

    [StringLength(PostTypeConstants.TypeNameMaxLength, MinimumLength = PostTypeConstants.TypeNameMinLength)]
    [Required]
    public required string Type { get; init; }

    public string? Slug { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = [];

    public string? Content { get; init; }
    public string? Status { get; init; }
    public string? Excerpt { get; init; }
    public string? LangCode { get; init; }
    public IReadOnlyCollection<Guid>? CategoryIds { get; init; }
    public IReadOnlyDictionary<string, JsonValue>? Meta { get; init; }

}

public record UpdatePostJsonRequest
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
    public required string? LangCode { get; init; }
    public IReadOnlyCollection<Guid>? CategoryIds { get; init; }
    public required IReadOnlyDictionary<string, JsonValue>? Meta { get; init; }

}
