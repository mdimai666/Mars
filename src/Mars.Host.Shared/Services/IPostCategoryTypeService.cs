using Mars.Host.Shared.Dto.PostCategoryTypes;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostCategoryTypes;

namespace Mars.Host.Shared.Services;

public interface IPostCategoryTypeService
{
    Task<PostCategoryTypeSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<PostCategoryTypeSummary?> GetByName(string typeName, CancellationToken cancellationToken);
    Task<PostCategoryTypeDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<PostCategoryTypeDetail?> GetDetailByName(string typeName, CancellationToken cancellationToken);
    Task<ListDataResult<PostCategoryTypeSummary>> List(ListPostCategoryTypeQuery query, CancellationToken cancellationToken);
    Task<PagingResult<PostCategoryTypeSummary>> ListTable(ListPostCategoryTypeQuery query, CancellationToken cancellationToken);

    Task<PostCategoryTypeDetail> Create(CreatePostCategoryTypeQuery query, CancellationToken cancellationToken);
    Task<PostCategoryTypeEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken);
    Task<PostCategoryTypeDetail> Update(UpdatePostCategoryTypeQuery query, CancellationToken cancellationToken);
    Task<PostCategoryTypeSummary> Delete(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PostCategoryTypeSummary>> DeleteMany(DeleteManyPostCategoryTypeQuery query, CancellationToken cancellationToken);

}
