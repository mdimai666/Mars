using Mars.Identity.Contracts.Roles;
using Mars.Identity.Contracts.Users;

namespace Mars.Identity.Contracts.ViewModels;

public class EditUserViewModel
{
    public required UserDetailResponse User { get; set; }
    public required IReadOnlyCollection<RoleSummaryResponse> Roles { get; set; }

}

public interface IViewModelBasic
{
    //public IViewModelBasic Create(IServiceProvider serviceProvider, ApplicationDbContext ef);
}
