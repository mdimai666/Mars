using System.ComponentModel.DataAnnotations;
using Mars.Shared.Contracts.Common;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Resources;

namespace Mars.Shared.Contracts.PostTypes;

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
    public required PostContentSettingsResponse PostContentSettings { get; init; }
    public required IReadOnlyCollection<MetaFieldDetailResponse> MetaFields { get; init; }

}

public record PostContentSettingsResponse
{
    /// <summary>
    /// <see cref="PostTypeConstants.DefaultPostContentTypes.PlainText"/>
    /// </summary>
    public required string PostContentType { get; init; }
    public required string? CodeLang { get; init; }
}


public record PostStatusResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

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
}
