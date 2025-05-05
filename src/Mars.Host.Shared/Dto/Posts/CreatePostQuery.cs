using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;
using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Shared.Dto.Posts;

public record CreatePostQuery : IGeneralPostQuery
{
    public Guid? Id { get; init; }
    public required string Title { get; init; }
    public required string Type { get; init; }

    //[SlugString(AllowUpperLetters = true)]
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required Guid UserId { get; init; }
    public required string? Status { get; init; }

    public required string? Content { get; init; }

    [Display(Name = "Отрывок")]
    public required string? Excerpt { get; init; }

    //[StringLength(5, MinimumLength = 2)]
    public required string LangCode { get; init; }

    public required IReadOnlyCollection<ModifyMetaValueDetailQuery> MetaValues { get; init; }
}
