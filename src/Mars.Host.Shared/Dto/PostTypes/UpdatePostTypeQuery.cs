using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Shared.Dto.PostTypes;

/// <summary>
/// <see cref="UpdatePostTypeRequest"/>
/// </summary>
public record UpdatePostTypeQuery
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<PostStatusDto> PostStatusList { get; init; }
    public required IReadOnlyCollection<string> EnabledFeatures { get; init; }
    public required bool Disabled { get; init; }
    public required PostContentSettingsDto PostContentSettings { get; init; }

    public required IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; }
}
