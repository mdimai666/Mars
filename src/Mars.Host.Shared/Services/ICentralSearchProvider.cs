using Mars.Host.Shared.Dto.Search;

namespace Mars.Host.Shared.Services;

/// <summary>
/// Источник поиска для палитры команд. Любой модуль или плагин может зарегистрировать
/// собственный провайдер в DI (<c>services.AddScoped&lt;ICentralSearchProvider, ...&gt;()</c>) —
/// агрегатор <see cref="ICentralSearchService"/> подхватит его автоматически.
/// </summary>
public interface ICentralSearchProvider
{
    /// <summary>
    /// Порядок выдачи в агрегированном результате: провайдеры с меньшим значением идут раньше.
    /// </summary>
    int Order { get; }

    Task<IReadOnlyCollection<SearchFoundElement>> SearchAsync(string query, int maxCount, CancellationToken cancellationToken);
}
