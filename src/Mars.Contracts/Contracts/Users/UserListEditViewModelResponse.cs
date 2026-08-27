using Mars.Shared.Common;
using Mars.Shared.Contracts.Roles;

namespace Mars.Shared.Contracts.Users;

public class UserListEditViewModelResponse
{
    public required ListDataResult<UserDetailResponse> Users { get; init; }
    public required IReadOnlyCollection<RoleSummaryResponse> Roles { get; init; }
    public required Guid? DefaultSelectRole { get; init; }
}
