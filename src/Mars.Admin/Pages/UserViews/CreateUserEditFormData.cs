using System.ComponentModel.DataAnnotations;
using Mars.Contracts.Roles;
using Mars.Contracts.UserTypes;

namespace Mars.Admin.Pages.UserViews;

public class CreateUserEditFormData
{
    public IReadOnlyCollection<RoleSummaryResponse> Roles { get; init; } = [];

    [ValidateComplexType]
    public CreateUserModel Model { get; init; } = new();

    public RoleSummaryResponse? DefaultCreateRole { get; set; } = default!;

    public IReadOnlyCollection<UserTypeListItemResponse> UserTypes { get; init; } = [];
}
