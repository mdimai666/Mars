using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Shared.Models;

namespace Mars.Host.Shared.Dto.PostTypes;

public record PostTypeDetail : PostTypeSummary
{
    public required DateTimeOffset? ModifiedAt { get; init; }

    public required IReadOnlyCollection<PostStatusDto> PostStatusList { get; init; }
    public required bool Disabled { get; init; }
    public required PostContentSettingsDto PostContentSettings { get; init; }
    public required IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; }

    public required PostTypePresentation Presentation { get; init; }
}

public record PostTypePresentation
{
    public required SourceUri? ListViewTemplate { get; init; }

    public static PostTypePresentation Default()
        => new()
        {
            ListViewTemplate = null
        };
}
