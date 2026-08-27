using Mars.Contracts.Roles;
using Mars.Contracts.Users;

namespace Mars.Contracts.ViewModels;

public class EditUserViewModel
{
    public required UserDetailResponse User { get; set; }
    public required IReadOnlyCollection<RoleSummaryResponse> Roles { get; set; }

}

public interface IViewModelBasic
{
    //public IViewModelBasic Create(IServiceProvider serviceProvider, ApplicationDbContext ef);
}
