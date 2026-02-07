using Mars.Host.Shared.Dto.PostCategoryTypes;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Repositories;

public interface IPostCategoryTypeRepository : IDisposable
{
    Task<PostCategoryTypeSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<PostCategoryTypeSummary?> GetByName(string name, CancellationToken cancellationToken);
    Task<PostCategoryTypeDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<PostCategoryTypeDetail?> GetDetailByName(string name, CancellationToken cancellationToken);
    Task<bool> TypeNameExist(string name, CancellationToken cancellationToken);

    Task<Guid> Create(CreatePostCategoryTypeQuery query, CancellationToken cancellationToken);
    Task Update(UpdatePostCategoryTypeQuery query, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PostCategoryTypeSummary>> ListAll(ListAllPostCategoryTypeQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PostCategoryTypeDetail>> ListAllDetail(ListAllPostCategoryTypeQuery query, CancellationToken cancellationToken);
    Task<ListDataResult<PostCategoryTypeSummary>> List(ListPostCategoryTypeQuery query, CancellationToken cancellationToken);
    Task<PagingResult<PostCategoryTypeSummary>> ListTable(ListPostCategoryTypeQuery query, CancellationToken cancellationToken);
    Task<int> DeleteMany(DeleteManyPostCategoryTypeQuery query, CancellationToken cancellationToken);
}
