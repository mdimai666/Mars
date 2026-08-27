using Mars.Contracts.PostTypes;

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

}
