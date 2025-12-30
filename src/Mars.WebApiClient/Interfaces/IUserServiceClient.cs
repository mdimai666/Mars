using Mars.Core.Exceptions;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Users;

namespace Mars.WebApiClient.Interfaces;

public interface IUserServiceClient
{
    Task<UserDetailResponse?> Get(Guid id);

    /// <summary>
    /// Создает
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="MarsValidationException"></exception>
    /// <exception cref="UserActionException"></exception>
    Task<UserDetailResponse> Create(CreateUserRequest request);
    Task<UserDetailResponse> Update(UpdateUserRequest request);
    Task<ListDataResult<UserListItemResponse>> List(ListUserQueryRequest filter);
    Task<ListDataResult<UserDetailResponse>> ListDetail(ListUserQueryRequest filter);
    Task<PagingResult<UserListItemResponse>> ListTable(TableUserQueryRequest filter);
    Task<PagingResult<UserDetailResponse>> ListTableDetail(TableUserQueryRequest filter);
    Task Delete(Guid id);
    Task DeleteMany(Guid[] ids);

    Task<UserActionResult> UpdateUserRoles(Guid id, IEnumerable<Guid> roles);
    Task<UserActionResult> SetPassword(SetUserPasswordByIdRequest auth);
    Task<UserActionResult> SendInvation(Guid id);
    Task<UserEditViewModel> GetEditModel(Guid id);
    Task<UserEditViewModel> GetUserBlank(string type);
}
