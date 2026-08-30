using Mars.Cms.Contracts.Search;

namespace Mars.WebApiClient.Interfaces;

public interface ISearchServiceClient
{
    /// <summary>
    /// Глобальный поиск по записям платформы (для палитры команд).
    /// </summary>
    Task<IReadOnlyCollection<SearchFoundElementResponse>> Query(string text, int maxCount = 20, CancellationToken cancellationToken = default);
}
