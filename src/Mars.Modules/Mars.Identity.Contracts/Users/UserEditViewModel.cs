using Mars.Identity.Contracts.Roles;
using Mars.Identity.Contracts.UserTypes;

namespace Mars.Identity.Contracts.Users;

public record UserEditViewModel
{
    public required UserEditResponse User { get; init; }
    public required UserTypeDetailResponse UserType { get; init; }
    public required IReadOnlyCollection<RoleSummaryResponse> AvailRoles { get; init; }
}
