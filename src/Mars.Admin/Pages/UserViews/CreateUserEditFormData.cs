using System.ComponentModel.DataAnnotations;
using Mars.Identity.Contracts.Roles;
using Mars.Identity.Contracts.UserTypes;

namespace Mars.Admin.Pages.UserViews;

public class CreateUserEditFormData
{
    public IReadOnlyCollection<RoleSummaryResponse> Roles { get; init; } = [];

    [ValidateComplexType]
    public CreateUserModel Model { get; init; } = new();

    public RoleSummaryResponse? DefaultCreateRole { get; set; } = default!;

    public IReadOnlyCollection<UserTypeListItemResponse> UserTypes { get; init; } = [];
}
