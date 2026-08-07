using Mars.Shared.Options;

namespace Mars.Host.Shared.Services;

/// <summary>
/// Синглтон. Актуальный список фронтов из <see cref="FrontsOption"/> (без рестарта приложения).
/// </summary>
public interface IFrontManager
{
    IReadOnlyList<FrontItem> Fronts { get; }

    /// <summary>
    /// Список фронтов изменился (сохранена FrontsOption)
    /// </summary>
    event Action? Changed;

    /// <summary>
    /// Фронт для URL запроса: наиболее специфичный маунт. null — фронт не найден/выключен.
    /// </summary>
    FrontItem? GetFrontForUrl(string url);

    /// <summary>
    /// Физический путь к папке фронта: data/fronts/&lt;slug&gt; или внешняя папка.
    /// </summary>
    string ResolvePhysicalPath(FrontItem front);
}
