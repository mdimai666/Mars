using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Contracts;

namespace Mars.Cms.Abstractions.Dto.PostTypes;

public record PostTypeDetail : PostTypeSummary
{
    public required DateTimeOffset? ModifiedAt { get; init; }

    public required IReadOnlyCollection<PostStatusDto> PostStatusList { get; init; }
    public required IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; }

    public required PostTypePresentation Presentation { get; init; }

    /// <summary>Сохранённая раскладка формы редактирования (<c>post_types.Options["form"]</c>); null — раскладка по умолчанию</summary>
    public FormLayoutSettings? Form { get; init; }

    /// <summary>Параметры системных полей (<c>post_types.Options["systemFields"]</c>); null — не заданы</summary>
    public IReadOnlyCollection<FormFieldSettings>? SystemFields { get; init; }
}

/// <summary>Системный слот контента (<see cref="SystemFieldsCatalog.Content"/>, фича «Контент»)</summary>
public static class PostTypeDetailContentExtensions
{
    /// <summary>Ключ редактора контента (пусто = обычный многострочный текст)</summary>
    public static string ContentEditorKey(this PostTypeDetail postType)
        => SystemFieldsCatalog.EditorKey(SystemFieldsCatalog.Content, postType.SystemFields);
}

public record PostTypePresentation
{
    /// <summary>
    /// Относительный путь к шаблону списка в специальном фронте админки
    /// (data/admin/front), например postTypes/article/listView.hbs. null — стандартный вывод.
    /// </summary>
    public required string? ListViewTemplate { get; init; }

    /// <summary>Настройки грида постов в админке; null — стандартный набор колонок</summary>
    public PostTypeGridSettings? Grid { get; init; }

    public static PostTypePresentation Default()
        => new()
        {
            ListViewTemplate = null,
            Grid = null,
        };
}
