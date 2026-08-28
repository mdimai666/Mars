using Mars.Admin.Framework.Models;
using Mars.Identity.Contracts.ViewModels;
using Mars.Server.Contracts.ViewModels;

namespace Mars.Admin.Framework.Mappings;

public static class ViewModelMapping
{
    public static AppInitialViewModel ToModel(this InitialSiteDataViewModel vm)
        => new()
        {
            SysOptions = vm.SysOptions,
            InitailUserPrimaryInfo = vm.UserPrimaryInfo,
            Options = vm.Options.ToList(),
            NavMenus = vm.NavMenus.ToList(),
            PostTypes = vm.PostTypes.ToList(),
            XActions = vm.XActions,
        };
}
