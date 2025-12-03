using Mars.Host.Shared.Dto.SSO;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Dto.Users.Passwords;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Users;

namespace Mars.Host.Shared.Services;

public interface IUserService
{
    Task<UserSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<UserDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<AuthorizedUserInformationDto?> GetAuthorizedUserInformation(string username, CancellationToken cancellationToken);
    Task<AuthorizedUserInformationDto?> GetAuthorizedUserInformation(Guid userId, CancellationToken cancellationToken);
    Task<ListDataResult<UserSummary>> List(ListUserQuery query, CancellationToken cancellationToken);
    Task<ListDataResult<UserDetail>> ListDetail(ListUserQuery query, CancellationToken cancellationToken);
    Task<PagingResult<UserSummary>> ListTable(ListUserQuery query, CancellationToken cancellationToken);
    Task<PagingResult<UserDetail>> ListTableDetail(ListUserQuery query, CancellationToken cancellationToken);

    Task<UserListEditViewModel> UsersEditViewModel(ListUserQuery query, CancellationToken cancellationToken);
    Task<UserDetail> Create(CreateUserQuery query, CancellationToken cancellationToken);
    Task<UserDetail> Update(UpdateUserQuery query, CancellationToken cancellationToken);
    Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken);
    Task<UserProfileInfoDto?> UserProfileInfo(Guid id, CancellationToken cancellationToken);
    Task<UserEditProfileDto?> UserEditProfileGet(Guid id, CancellationToken cancellationToken);
    //Task<UserEditProfileDto> UserEditProfileForAdminGet(Guid id);
    Task<UserActionResult<UserEditProfileDto>> UserEditProfileUpdate(UserEditProfileDto profile, CancellationToken cancellationToken);
    //Task<UserActionResult<UserEditProfileDto>> UserEditProfileForAdminUpdate(UserEditProfileForAdminDto profile);
    Task<UserActionResult> UpdateUserRoles(Guid id, IReadOnlyCollection<string> roles, CancellationToken cancellationToken);
    Task<UserActionResult> SetPassword(SetUserPasswordQuery query, CancellationToken cancellationToken);
    Task<UserActionResult> SetPassword(SetUserPasswordByIdQuery query, CancellationToken cancellationToken);
    Task<UserActionResult> SendInvation(Guid userId, CancellationToken cancellationToken);
    Task<CreateUserQuery> EnrichQuery(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UpdateUserQuery> EnrichQuery(UpdateUserRequest request, CancellationToken cancellationToken);
    Task<UserEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken);
    Task<UserEditViewModel> GetEditModelBlank(string type, CancellationToken cancellationToken);
    Task<AuthorizedUserInformationDto?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<AuthorizedUserInformationDto> RemoteUserUpsert(UpsertUserRemoteDataQuery query, CancellationToken cancellationToken);
}
