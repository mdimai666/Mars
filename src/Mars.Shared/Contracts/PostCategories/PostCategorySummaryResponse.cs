using System.ComponentModel.DataAnnotations;
using Mars.Shared.Contracts.Common;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Shared.Contracts.PostCategories;

public record PostCategorySummaryResponse : IBasicEntityResponse
{
    [Display(Name = "ИД")]
    public required Guid Id { get; init; }

    [Display(Name = "Дата создания")]
    public required DateTimeOffset CreatedAt { get; init; }

    [Display(Name = "Название")]
    public required string Title { get; init; }

    [StringLength(100)]
    [Display(Name = "Тип")]
    public required string TypeName { get; init; }

    [StringLength(100)]
    [Display(Name = "Slug")]
    public required string Slug { get; init; }

    [StringLength(100)]
    public required string PostType { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    /// <summary>
    /// /{rootId}/{parentId}/{id}/
    /// </summary>
    public required string Path { get; init; } = default!;

    /// <summary>
    /// /news/tech/ai
    /// </summary>
    public required string SlugPath { get; init; } = default!;
    public required Guid[] PathIds { get; init; } = [];
    public required int LevelsCount { get; init; }
}

public record PostCategoryDetailResponse : PostCategorySummaryResponse
{
    public required bool Disabled { get; init; }

    public required IReadOnlyCollection<MetaValueResponse> MetaValues { get; init; }

}

public record PostCategoryListItemResponse : IBasicEntityResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Title { get; init; }
    public required string Type { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    [StringLength(100)]
    public required string PostType { get; init; }

    /// <summary>
    /// /{rootId}/{parentId}/{id}/
    /// </summary>
    public required string Path { get; init; } = default!;

    /// <summary>
    /// /news/tech/ai
    /// </summary>
    public required string SlugPath { get; init; } = default!;
    public required Guid[] PathIds { get; init; } = [];
    public required int LevelsCount { get; init; }
}
