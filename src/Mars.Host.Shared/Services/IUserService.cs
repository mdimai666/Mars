using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Dto.Users.Passwords;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Services;

public interface IUserService //: ICurrentUserProvider
{
    //Task<UserListEditViewModel> UsersEditViewModel();
    //Task<List<UserRoleDto>> UserWithRoles(Expression<Func<User, bool>>? predicate = null);
    //Task<UserRoleDto> UserWithRolesOne(Guid userId);
    //Task<UserRoleDto> UserWithRolesOne(IMarsDbContext ef, Guid userId);
    //Task<UserActionResult<User>> CreateUser(CreateUserRequest dto, string? username = null, Guid? userId = null);
    //UserProfileInfoDto UserProfileInfo(Guid id);
    //UserEditProfileDto UserEditProfileGet(Guid id);
    //Task<UserActionResult<UserEditProfileDto>> UserEditProfileUpdate(UserEditProfileDto profile);
    //Task<UserActionResult> UpdateUserRoles(Guid id, IEnumerable<Guid> roles);
    //UserActionResult SetUserState(Guid id, bool state);
    //Task<UserActionResult> SetPassword(AuthCreditionalsDto auth);
    //Task<UserActionResult> SendInvation(Guid id);
    //List<MetaField> UserMetaFields(IMarsDbContext ef);
    //Task<UserActionResult<List<MetaField>>> UserMetaFields(List<MetaField> entity_MetaFields);

    //-------------------
    Task<UserSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<UserDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<ListDataResult<UserSummary>> List(ListUserQuery query, CancellationToken cancellationToken);
    Task<ListDataResult<UserDetail>> ListDetail(ListUserQuery query, CancellationToken cancellationToken);
    Task<PagingResult<UserSummary>> ListTable(ListUserQuery query, CancellationToken cancellationToken);
    Task<PagingResult<UserDetail>> ListTableDetail(ListUserQuery query, CancellationToken cancellationToken);

    Task<UserListEditViewModel> UsersEditViewModel(ListUserQuery query, CancellationToken cancellationToken);
    //Task<List<UserRoleDto>> UserWithRoles(Expression<Func<User, bool>> predicate = null);
    //Task<UserRoleDto> UserWithRolesOne(Guid userId);
    //Task<UserRoleDto?> UserWithRolesOne(IMarsDbContext ef, Guid userId);
    //Task<UserActionResult<User>> CreateUser(CreateUserQuery createQuery, string? username = null, Guid? userId = null, CancellationToken cancellationToken);
    Task<UserDetail> Create(CreateUserQuery query, CancellationToken cancellationToken);
    Task<UserDetail> Update(UpdateUserQuery query, CancellationToken cancellationToken);
    Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken);
    Task<UserProfileInfoDto?> UserProfileInfo(Guid id, CancellationToken cancellationToken);
    Task<UserEditProfileDto?> UserEditProfileGet(Guid id, CancellationToken cancellationToken);
    //Task<UserEditProfileDto> UserEditProfileForAdminGet(Guid id);
    Task<UserActionResult<UserEditProfileDto>> UserEditProfileUpdate(UserEditProfileDto profile, CancellationToken cancellationToken);
    //Task<UserActionResult<UserEditProfileDto>> UserEditProfileForAdminUpdate(UserEditProfileForAdminDto profile);
    Task<UserActionResult> UpdateUserRoles(Guid id, IReadOnlyCollection<string> roles, CancellationToken cancellationToken);
    //UserActionResult SetUserState(Guid id, bool state);
    Task<UserActionResult> SetPassword(SetUserPasswordQuery query, CancellationToken cancellationToken);
    Task<UserActionResult> SetPassword(SetUserPasswordByIdQuery query, CancellationToken cancellationToken);
    Task<UserActionResult> SendInvation(Guid userId, CancellationToken cancellationToken);
    IReadOnlyCollection<MetaFieldDto> UserMetaFields(object ef);
    Task<IReadOnlyCollection<MetaFieldDto>> UserMetaFields(IReadOnlyCollection<MetaFieldDto> entity_MetaFields);
    //Task UpdateUserMetaValues(Guid userId, ICollection<MetaValue> fromDbMetaValues, ICollection<MetaValue> entity_MetaValues, ICollection<MetaField> metaFields);

    //JObject AsJson22(object pctx, object userWithMetaValues);

}
