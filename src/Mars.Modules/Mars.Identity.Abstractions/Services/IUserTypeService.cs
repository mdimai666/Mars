using Mars.Identity.Abstractions.Dto.UserTypes;
using Mars.Contracts.Common;
using Mars.Contracts.UserTypes;

namespace Mars.Identity.Abstractions.Services;

public interface IUserTypeService
{
    Task<UserTypeSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<UserTypeDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<ListDataResult<UserTypeSummary>> List(ListUserTypeQuery query, CancellationToken cancellationToken);
    Task<PagingResult<UserTypeSummary>> ListTable(ListUserTypeQuery query, CancellationToken cancellationToken);

    Task<UserTypeDetail> Create(CreateUserTypeQuery query, CancellationToken cancellationToken);
    Task<UserTypeEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken);
    Task<UserTypeDetail> Update(UpdateUserTypeQuery query, CancellationToken cancellationToken);
    Task<UserTypeSummary> Delete(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<UserTypeSummary>> DeleteMany(DeleteManyUserTypeQuery query, CancellationToken cancellationToken);
}
