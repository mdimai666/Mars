using Mars.Shared.Contracts.Roles;
using Mars.Shared.Contracts.Users;

namespace Mars.Shared.ViewModels;

public class EditUserViewModel
{
    public UserDetailResponse User { get; set; }
    public IReadOnlyCollection<RoleSummaryResponse> Roles { get; set; }

}

public interface IViewModelBasic
{
    //public IViewModelBasic Create(IServiceProvider serviceProvider, ApplicationDbContext ef);
}
