using Mars.Host.Shared.Dto.Search;

namespace Mars.Host.Shared.Services;

public interface ICentralSearchService
{
    Task<IReadOnlyCollection<SearchFoundElement>> ActionBarSearch(string query, int maxCount, CancellationToken cancellationToken);
}
