namespace Mars.Plugin.Contracts.Catalog;

/// <summary>Отзыв о плагине из внешнего каталога (зеркало контракта каталога).</summary>
public sealed record CatalogReviewDto(
    int Id,
    string UserSub,
    string UserName,
    int Rating,
    string? Text,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
