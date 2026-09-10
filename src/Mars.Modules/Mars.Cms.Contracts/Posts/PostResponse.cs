using System.ComponentModel.DataAnnotations;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostCategories;
using Mars.Contracts.Common;

namespace Mars.Cms.Contracts.Posts;

public record PostSummaryResponse : IBasicEntityResponse
{
    [Display(Name = "ИД")]
    public required Guid Id { get; init; }

    [Display(Name = "Дата создания")]
    public required DateTimeOffset CreatedAt { get; init; }

    [Display(Name = "Название")]
    public required string Title { get; init; }

    [StringLength(100)]
    [Display(Name = "Тип")]
    public required string Type { get; init; }

    [StringLength(100)]
    [Display(Name = "Slug")]
    public required string Slug { get; init; }

    public required IReadOnlyCollection<string> Tags { get; init; }

    public required PostAuthorResponse Author { get; init; }
    public required IReadOnlyCollection<PostCategorySummaryResponse>? Categories { get; init; }
}

public record PostDetailResponse : PostSummaryResponse
{
    [Display(Name = "Текст")]
    public required string? Content { get; init; }

    /// <summary>Значения мета-полей: ключ поля → список значений (по <see cref="MetaValueResponse.Index"/>)</summary>
    public required IReadOnlyDictionary<string, IReadOnlyList<MetaValueResponse>> MetaValues { get; init; }
}

public record PostListItemResponse : IBasicEntityResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    //public required DateTime ModifiedAt { get; init; }
    public required string Title { get; init; }
    public required string Type { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required PostAuthorResponse Author { get; init; }
    public required IReadOnlyCollection<PostCategorySummaryResponse>? Categories { get; init; }
    public required KeyValuePair<string, string>? Status { get; init; }

    /// <summary>Отображаемые значения запрошенных мета-полей (ключ поля → значение) для колонок грида</summary>
    public IReadOnlyDictionary<string, string?>? MetaColumns { get; init; }
}
