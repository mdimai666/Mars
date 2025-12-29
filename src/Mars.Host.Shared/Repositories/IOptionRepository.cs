using Mars.Host.Shared.Dto.Options;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Repositories;

public interface IOptionRepository : IDisposable
{
    Task<T?> Get<T>(Guid id, CancellationToken cancellationToken = default)
        where T : class;
    Task<T?> GetKey<T>(string key, CancellationToken cancellationToken = default)
        where T : class;

    Task<OptionDetail?> GetKeyRaw(string key, CancellationToken cancellationToken = default);
    Task<Guid> Create<T>(CreateOptionQuery<T> query, CancellationToken cancellationToken)
        where T : class;
    Task Update<T>(UpdateOptionQuery<T> query, CancellationToken cancellationToken)
        where T : class;
    Task Delete(Guid id, CancellationToken cancellationToken);
    Task Delete(string key, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OptionSummary>> ListAll(CancellationToken cancellationToken);
    Task<ListDataResult<OptionSummary>> List(ListOptionQuery query, CancellationToken cancellationToken);
    Task<PagingResult<OptionSummary>> ListTable(ListOptionQuery query, CancellationToken cancellationToken);
    Task<int> DeleteMany(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
