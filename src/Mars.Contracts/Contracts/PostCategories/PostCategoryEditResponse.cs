using System.ComponentModel.DataAnnotations;
using Mars.Shared.Contracts.Common;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Resources;

namespace Mars.Shared.Contracts.PostCategories;

public record PostCategoryEditResponse : IBasicEntityResponse
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
    [Display(Name = nameof(AppRes.PostType), ResourceType = typeof(AppRes))]
    public required string PostType { get; init; }

    [StringLength(100)]
    [Display(Name = "Slug")]
    public required string Slug { get; init; }

    public required Guid? ParentId { get; init; }
    public required Guid[] PathIds { get; init; } = [];
    public required bool Disabled { get; init; }

    public required IReadOnlyCollection<string> Tags { get; init; }
    public required IReadOnlyCollection<MetaValueDetailResponse> MetaValues { get; init; }

}
