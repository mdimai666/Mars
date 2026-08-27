using Mars.Host.Shared.Dto.Roles;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Dto.Users;

public class UserListEditViewModel
{
    public required ListDataResult<UserDetail> Users { get; init; }
    public required IReadOnlyCollection<RoleSummary> Roles { get; init; }
    public required Guid? DefaultSelectRole { get; init; }
}
