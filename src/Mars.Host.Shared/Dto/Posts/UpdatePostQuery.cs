using System.ComponentModel.DataAnnotations;
using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Shared.Dto.Posts;

public record UpdatePostQuery : IGeneralPostQuery
{
    public required Guid Id { get; init; }
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

    public required IReadOnlyCollection<ModifyMetaValueDetailQuery>? MetaValues { get; init; }
}
