using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Repositories;

public interface IPostTypeRepository
{
    Task<PostTypeSummary?> Get(Guid id, CancellationToken cancellationToken);

    Task<PostTypeSummary?> GetByName(string name, CancellationToken cancellationToken);

    /// <summary>
    /// for best perfomance use <see cref="IMetaModelTypesLocator.GetPostTypeByName"/>
    /// </summary>
    Task<PostTypeDetail?> GetDetail(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// for best perfomance use <see cref="IMetaModelTypesLocator.GetPostTypeByName"/>
    /// </summary>
    Task<PostTypeDetail?> GetDetailByName(string name, CancellationToken cancellationToken);

    Task<Guid> Create(CreatePostTypeQuery query, CancellationToken cancellationToken);
    /// <summary>
    /// Update
    /// </summary>
    /// <exception cref="NotFoundException"/>
    Task Update(UpdatePostTypeQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Delete
    /// </summary>
    /// <exception cref="NotFoundException"/>
    Task Delete(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PostTypeSummary>> ListAll(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PostTypeSummary>> ListAllActive(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PostTypeDetail>>  ListAllDetail(CancellationToken cancellationToken);
    Task<ListDataResult<PostTypeSummary>> List(ListPostTypeQuery query, CancellationToken cancellationToken);
    Task<PagingResult<PostTypeSummary>> ListTable(ListPostTypeQuery query, CancellationToken cancellationToken);
    Task<bool> TypeNameExist(string name, CancellationToken cancellationToken);
}
