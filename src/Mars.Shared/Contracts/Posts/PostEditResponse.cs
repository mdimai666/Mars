using System.ComponentModel.DataAnnotations;
using Mars.Shared.Contracts.Common;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Shared.Contracts.Posts;

public record PostEditResponse : IBasicEntityResponse
{
    [Display(Name = "ИД")]
    public required Guid Id { get; init; }

    [Display(Name = "Дата создания")]
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? ModifiedAt { get; init; }

    [Display(Name = "Название")]
    public required string Title { get; init; }

    [StringLength(100)]
    [Display(Name = "Тип")]
    public required string Type { get; init; }

    [StringLength(100)]
    [Display(Name = "Slug")]
    public required string Slug { get; init; }

    public required string Status { get; init; }

    public required PostAuthorResponse Author { get; init; }

    [Display(Name = "Текст")]
    public required string? Content { get; init; }
    public required string? Excerpt { get; init; }
    public required string LangCode { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required IReadOnlyCollection<Guid> CategoryIds { get; init; }
    public required IReadOnlyCollection<MetaValueDetailResponse> MetaValues { get; init; }

}
