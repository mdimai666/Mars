using Mars.Contracts.Common;
using Mars.Contracts.Roles;

namespace Mars.Contracts.Users;

public class UserListEditViewModelResponse
{
    public required ListDataResult<UserDetailResponse> Users { get; init; }
    public required IReadOnlyCollection<RoleSummaryResponse> Roles { get; init; }
    public required Guid? DefaultSelectRole { get; init; }
}
