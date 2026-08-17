using Mars.Host.Shared.Dto.Search;
using Mars.Host.Shared.Services;

namespace Mars.Host.Services;

/// <summary>
/// Агрегатор поиска: опрашивает все зарегистрированные <see cref="ICentralSearchProvider"/>
/// и сводит их выдачу в один список. Провайдеры упорядочиваются по <see cref="ICentralSearchProvider.Order"/>,
/// внутри провайдера — по убыванию <see cref="SearchFoundElement.Relevant"/>.
/// </summary>
internal class CentralSearchService(
    IEnumerable<ICentralSearchProvider> _providers
    ) : ICentralSearchService
{
    public async Task<IReadOnlyCollection<SearchFoundElement>> ActionBarSearch(string query, int maxCount, CancellationToken cancellationToken)
    {
        var orderedProviders = _providers
            .OrderBy(p => p.Order)
            .ToList();

        var results = await Task.WhenAll(
            orderedProviders.Select(p => p.SearchAsync(query, maxCount, cancellationToken)));

        var items = orderedProviders
            .Zip(results)
            .SelectMany(pair => pair.Second.OrderByDescending(el => el.Relevant))
            .Take(maxCount)
            .ToList();

        return items;
    }
}
