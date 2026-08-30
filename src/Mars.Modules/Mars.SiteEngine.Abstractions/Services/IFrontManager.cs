using Mars.SiteEngine.Contracts.Options;

namespace Mars.SiteEngine.Abstractions.Services;

/// <summary>
/// Синглтон. Актуальный список фронтов из <see cref="FrontsOption"/> (без рестарта приложения).
/// </summary>
public interface IFrontManager
{
    IReadOnlyList<FrontItem> Fronts { get; }

    /// <summary>
    /// Специальный фронт админки (data/admin/front). Не входит в <see cref="Fronts"/>,
    /// не роутится публично — шаблоны отображаются только в админ-панели.
    /// </summary>
    FrontItem AdminFront { get; }

    /// <summary>
    /// Список фронтов изменился (сохранена FrontsOption)
    /// </summary>
    event Action? Changed;

    /// <summary>
    /// Фронт для URL запроса: наиболее специфичный маунт. null — фронт не найден/выключен.
    /// </summary>
    FrontItem? GetFrontForUrl(string url);

    /// <summary>
    /// Фронт по slug: ищет в <see cref="Fronts"/> и в специальном админ-фронте. null — не найден.
    /// </summary>
    FrontItem? FindBySlug(string slug);

    /// <summary>
    /// Физический путь к папке фронта: data/fronts/&lt;slug&gt; или внешняя папка.
    /// </summary>
    string ResolvePhysicalPath(FrontItem front);
}
