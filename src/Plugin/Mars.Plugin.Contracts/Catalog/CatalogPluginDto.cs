namespace Mars.Plugin.Contracts.Catalog;

/// <summary>
/// Карточка плагина внешнего каталога. Зеркало контракта каталога
/// (см. репозиторий Mars.Cloud, <c>Mars.PluginCatalog</c>) — Марс не ссылается
/// на каталог, поэтому модель продублирована здесь.
/// </summary>
public sealed record CatalogPluginDto(
    string PackageId,
    string DisplayName,
    string? Summary,
    string? Description,
    string? AuthorName,
    string? RepositoryUrl,
    string? HomepageUrl,
    string? LicenseUrl,
    string? IconUrl,
    IReadOnlyList<string> Tags,
    string Status,
    bool IsRecommended,
    string? MarsVersionMin,
    string? MarsVersionMax,
    string? LatestVersion,
    long TotalDownloads,
    double AvgRating,
    int ReviewsCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
