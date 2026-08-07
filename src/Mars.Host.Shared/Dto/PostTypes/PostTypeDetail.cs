using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;

namespace Mars.Host.Shared.Dto.PostTypes;

public record PostTypeDetail : PostTypeSummary
{
    public required DateTimeOffset? ModifiedAt { get; init; }

    public required IReadOnlyCollection<PostStatusDto> PostStatusList { get; init; }
    public required PostContentSettingsDto PostContentSettings { get; init; }
    public required IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; }

    public required PostTypePresentation Presentation { get; init; }
}

public record PostTypePresentation
{
    /// <summary>
    /// Относительный путь к шаблону списка в специальном фронте админки
    /// (data/admin/front), например postTypes/article/listView.hbs. null — стандартный вывод.
    /// </summary>
    public required string? ListViewTemplate { get; init; }

    public static PostTypePresentation Default()
        => new()
        {
            ListViewTemplate = null
        };
}
