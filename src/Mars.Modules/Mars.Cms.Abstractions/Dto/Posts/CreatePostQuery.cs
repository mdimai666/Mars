using System.ComponentModel.DataAnnotations;
using Mars.Cms.Abstractions.Dto.MetaFields;

namespace Mars.Cms.Abstractions.Dto.Posts;

public record CreatePostQuery : IGeneralPostQuery
{
    public Guid? Id { get; init; }
    public required string Title { get; init; }
    public required string Type { get; init; }

    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required Guid UserId { get; init; }
    public required string? Status { get; init; }

    public required string? Content { get; init; }

    [Display(Name = "Отрывок")]
    public required string? Excerpt { get; init; }

    public required string LangCode { get; init; }

    public required IReadOnlyCollection<Guid> CategoryIds { get; init; }

    public required IReadOnlyCollection<ModifyMetaValueDetailQuery> MetaValues { get; init; }
}
