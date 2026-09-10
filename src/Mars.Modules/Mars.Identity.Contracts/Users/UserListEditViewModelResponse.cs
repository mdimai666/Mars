using Mars.Contracts.Common;
using Mars.Identity.Contracts.Roles;

namespace Mars.Identity.Contracts.Users;

public class UserListEditViewModelResponse
{
    public required ListDataResult<UserDetailResponse> Users { get; init; }
    public required IReadOnlyCollection<RoleSummaryResponse> Roles { get; init; }
    public required Guid? DefaultSelectRole { get; init; }
}
