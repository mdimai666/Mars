using Mars.Identity.Abstractions.Dto.Roles;
using Mars.Contracts.Common;

namespace Mars.Identity.Abstractions.Dto.Users;

public class UserListEditViewModel
{
    public required ListDataResult<UserDetail> Users { get; init; }
    public required IReadOnlyCollection<RoleSummary> Roles { get; init; }
    public required Guid? DefaultSelectRole { get; init; }
}
