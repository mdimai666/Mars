using Mars.Cms.Abstractions.Dto.Search;

namespace Mars.Cms.Abstractions.Services;

public interface ICentralSearchService
{
    Task<IReadOnlyCollection<SearchFoundElement>> ActionBarSearch(string query, int maxCount, CancellationToken cancellationToken);
}
