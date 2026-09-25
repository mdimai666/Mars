using System.ComponentModel.DataAnnotations;
using Mars.Cms.Contracts.MetaFields;
using Mars.Contracts.Common;
using Mars.Contracts.Resources;
using Mars.Forms.Contracts;

namespace Mars.Cms.Contracts.PostTypes;

public record PostTypeSummaryResponse : IBasicEntityResponse
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

    public required IReadOnlyCollection<string> EnabledFeatures { get; init; }

    [Display(Name = "Видимость")]
    public required PostTypeVisibility Visibility { get; init; }

    public string? ImageFieldKey { get; init; }

    //extra
    //public required IReadOnlyCollection<MetaFieldResponse> MetaFields { get; init; }

}

public record PostTypeDetailResponse : IBasicEntityResponse
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

    //details
    [Display(Name = nameof(AppRes.ModifiedAt), ResourceType = typeof(AppRes))]
    public required DateTimeOffset? ModifiedAt { get; init; }

    public required IReadOnlyCollection<PostStatusResponse> PostStatusList { get; init; }
    public required IReadOnlyCollection<string> EnabledFeatures { get; init; }
    public required bool Disabled { get; init; }

    [Display(Name = "Видимость")]
    public required PostTypeVisibility Visibility { get; init; }
    public required IReadOnlyCollection<MetaFieldDetailResponse> MetaFields { get; init; }

    public string? ImageFieldKey { get; init; }

    /// <summary>
    /// Параметры системных полей (правила, редактор) — <c>post_types.Options["systemFields"]</c>;
    /// null — не заданы. Симметрично <see cref="CreatePostTypeRequest.SystemFields"/> и
    /// <see cref="UpdatePostTypeRequest.SystemFields"/>: что принимает запись, то отдаёт чтение.
    /// </summary>
    public IReadOnlyCollection<FormFieldSettings>? SystemFields { get; init; }

}

public record PostStatusResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required string Color { get; init; }
    public required int Order { get; init; }

}

public class PostTypeEditResponse : IBasicEntityResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public record PostTypeListItemResponse : IBasicEntityResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    //public required DateTimeOffset ModifiedAt { get; init; }
    public required string Title { get; init; }
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> EnabledFeatures { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required bool Disabled { get; init; }
    public required PostTypeVisibility Visibility { get; init; }

    public string? ImageFieldKey { get; init; }
}

public record PostTypeAdminPanelItemResponse : PostTypeSummaryResponse
{
    public required PostTypePresentationResponse Presentation { get; init; }

}
