using Mars.Shared.Contracts.Roles;
using Mars.Shared.Contracts.UserTypes;

namespace Mars.Shared.Contracts.Users;

public record UserEditViewModel
{
    public required UserEditResponse User { get; init; }
    public required UserTypeDetailResponse UserType { get; init; }
    public required IReadOnlyCollection<RoleSummaryResponse> AvailRoles { get; init; }
}
