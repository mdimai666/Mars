using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Dto.Users.Passwords;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Repositories;

public interface IUserRepository : IDisposable
{
    Task<UserSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<UserDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<UserDetail?> GetDetailByUserName(string username, CancellationToken cancellationToken);
    Task<UserEditDetail?> GetUserEditDetail(Guid id, CancellationToken cancellationToken);
    Task<Guid> Create(CreateUserQuery query, CancellationToken cancellationToken);
    Task Update(UpdateUserQuery query, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserSummary>> ListAll(ListAllUserQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<UserDetail>> ListAllDetail(ListAllUserQuery query, CancellationToken cancellationToken);
    Task<ListDataResult<UserSummary>> List(ListUserQuery query, CancellationToken cancellationToken);
    Task<ListDataResult<UserDetail>> ListDetail(ListUserQuery query, CancellationToken cancellationToken);
    Task<PagingResult<UserSummary>> ListTable(ListUserQuery query, CancellationToken cancellationToken);
    Task<PagingResult<UserDetail>> ListTableDetail(ListUserQuery query, CancellationToken cancellationToken);

    Task<UserActionResult> SetPassword(SetUserPasswordQuery query, CancellationToken cancellationToken);
    Task<UserActionResult> SetPassword(SetUserPasswordByIdQuery query, CancellationToken cancellationToken);
    Task<UserEditProfileDto?> UserEditProfileGet(Guid id, CancellationToken cancellationToken);
    Task<UserActionResult> UpdateUserRoles(Guid userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken);
}
