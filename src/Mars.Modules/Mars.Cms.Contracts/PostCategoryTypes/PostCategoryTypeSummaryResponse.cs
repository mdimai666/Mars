using System.ComponentModel.DataAnnotations;
using Mars.Contracts.Common;
using Mars.Cms.Contracts.MetaFields;
using Mars.Contracts.Resources;

namespace Mars.Cms.Contracts.PostCategoryTypes;

public record PostCategoryTypeSummaryResponse : IBasicEntityResponse
{
    [Display(Name = "ИД")]
    public required Guid Id { get; init; }

    [Display(Name = nameof(AppRes.CreatedAt), ResourceType = typeof(AppRes))]
    public required DateTimeOffset CreatedAt { get; init; }

    [Display(Name = nameof(AppRes.Title), ResourceType = typeof(AppRes))]
    public required string Title { get; init; }

    [StringLength(100)]
    [Display(Name = "Тип")]
    public required string TypeName { get; init; }

    [Display(Name = nameof(AppRes.Tags), ResourceType = typeof(AppRes))]
    public required IReadOnlyCollection<string> Tags { get; init; }

}

public record PostCategoryTypeDetailResponse : PostCategoryTypeSummaryResponse //IBasicEntityResponse
{
    [Display(Name = nameof(AppRes.ModifiedAt), ResourceType = typeof(AppRes))]
    public required DateTimeOffset? ModifiedAt { get; init; }

    public required IReadOnlyCollection<MetaFieldDetailResponse> MetaFields { get; init; }

}

public class PostCategoryTypeEditResponse : IBasicEntityResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public record PostCategoryTypeListItemResponse : IBasicEntityResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Title { get; init; }
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
}
