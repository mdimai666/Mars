using Mars.Identity.Abstractions.Dto.Profile;
using Mars.Identity.Abstractions.Dto.SSO;
using Mars.Identity.Abstractions.Dto.Users;
using Mars.Identity.Abstractions.Dto.Users.Passwords;
using Mars.Contracts.Common;

namespace Mars.Identity.Abstractions.Repositories;

public interface IUserRepository : IDisposable
{
    Task<UserSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<UserDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<UserDetail?> GetDetailByUserName(string username, CancellationToken cancellationToken);
    Task<UserEditDetail?> GetUserEditDetail(Guid id, CancellationToken cancellationToken);
    Task<AuthorizedUserInformationDto?> GetAuthorizedUserInformation(string username, CancellationToken cancellationToken);
    Task<AuthorizedUserInformationDto?> GetAuthorizedUserInformation(Guid userId, CancellationToken cancellationToken);

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
    Task<UserProfileDto?> UserProfile(Guid id, CancellationToken cancellationToken);
    Task<UserEditProfileDto?> UserEditProfileGet(Guid id, CancellationToken cancellationToken);
    Task<UserActionResult> UpdateUserRoles(Guid userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken);
    Task<AuthorizedUserInformationDto?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<AuthorizedUserInformationDto> RemoteUserUpsert(UpsertUserRemoteDataQuery query, CancellationToken cancellationToken);
    Task<bool> UserNameExistAsync(string username, CancellationToken cancellationToken);
    Task<bool> UsernameIsAlreadyTakenByAnotherUser(string newUsername, Guid userId, CancellationToken cancellationToken);
    Task<int> DeleteMany(DeleteManyUserQuery query, CancellationToken cancellationToken);
}
