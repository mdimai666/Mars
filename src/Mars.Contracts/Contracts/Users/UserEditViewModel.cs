using Mars.Contracts.Roles;
using Mars.Contracts.UserTypes;

namespace Mars.Contracts.Users;

public record UserEditViewModel
{
    public required UserEditResponse User { get; init; }
    public required UserTypeDetailResponse UserType { get; init; }
    public required IReadOnlyCollection<RoleSummaryResponse> AvailRoles { get; init; }
}
