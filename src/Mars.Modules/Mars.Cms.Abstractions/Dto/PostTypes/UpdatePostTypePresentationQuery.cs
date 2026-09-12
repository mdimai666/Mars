using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Contracts;

namespace Mars.Cms.Abstractions.Dto.PostTypes;

public record UpdatePostTypePresentationQuery
{
    public required Guid Id { get; init; }

    /// <summary>
    /// Относительный путь к шаблону списка во фронте админки (data/admin/front).
    /// </summary>
    public required string ListViewTemplate { get; init; }

    /// <summary>Настройки грида постов в админке; null — стандартный набор колонок</summary>
    public PostTypeGridSettings? Grid { get; init; }

    /// <summary>Раскладка формы редактирования поста; null — раскладка по умолчанию</summary>
    public FormLayoutSettings? Form { get; init; }

}
