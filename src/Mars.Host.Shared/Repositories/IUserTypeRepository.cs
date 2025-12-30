using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.UserTypes;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Repositories;

public interface IUserTypeRepository : IDisposable
{
    Task<UserTypeSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<UserTypeSummary?> GetByName(string name, CancellationToken cancellationToken);
    Task<UserTypeDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<UserTypeDetail?> GetDetailByName(string name, CancellationToken cancellationToken);
    Task<bool> TypeNameExist(string name, CancellationToken cancellationToken);
    Task<Guid> Create(CreateUserTypeQuery query, CancellationToken cancellationToken);

    /// <exception cref="NotFoundException"/>
    Task Update(UpdateUserTypeQuery query, CancellationToken cancellationToken);

    /// <exception cref="NotFoundException"/>
    Task Delete(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserTypeSummary>> ListAll(ListAllUserTypeQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<UserTypeDetail>> ListAllDetail(ListAllUserTypeQuery query, CancellationToken cancellationToken);
    Task<ListDataResult<UserTypeSummary>> List(ListUserTypeQuery query, CancellationToken cancellationToken);
    Task<PagingResult<UserTypeSummary>> ListTable(ListUserTypeQuery query, CancellationToken cancellationToken);
    Task<int> DeleteMany(DeleteManyUserTypeQuery query, CancellationToken cancellationToken);
}
