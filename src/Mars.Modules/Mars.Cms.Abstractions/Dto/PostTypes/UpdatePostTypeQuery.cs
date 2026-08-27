using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Contracts.PostTypes;

namespace Mars.Cms.Abstractions.Dto.PostTypes;

/// <summary>
/// <see cref="UpdatePostTypeRequest"/>
/// </summary>
public record UpdatePostTypeQuery : IGeneralPostTypeQuery, IGeneralMetaFieldsSupportDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<PostStatusDto> PostStatusList { get; init; }
    public required IReadOnlyCollection<string> EnabledFeatures { get; init; }
    public required bool Disabled { get; init; }
    public required PostTypeVisibility Visibility { get; init; }

    public required IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; }
    public string? ImageFieldKey { get; init; }
}
