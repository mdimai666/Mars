using Mars.Cms.Abstractions.Dto.Search;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Host.Services;

namespace Mars.Cms.Host.Services;

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

        try
        {
            // Последовательный вызов: провайдеры scoped и делят один MarsDbContext из pool,
            // параллельный Task.WhenAll приводил к InvalidOperationException
            // ("A second operation was started on this context instance before a previous operation completed").
            var results = new List<IReadOnlyCollection<SearchFoundElement>>();
            foreach (var provider in orderedProviders)
            {
                results.Add(await provider.SearchAsync(query, maxCount, cancellationToken));
            }

            var items = orderedProviders
                .Zip(results)
                .SelectMany(pair => pair.Second.OrderByDescending(el => el.Relevant))
                .Take(maxCount)
                .ToList();

            return items;
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }
}
