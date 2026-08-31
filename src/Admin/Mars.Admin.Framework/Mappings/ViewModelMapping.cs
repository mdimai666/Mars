using Mars.Admin.Framework.Models;
using Mars.Admin.Contracts.ViewModels;

namespace Mars.Admin.Framework.Mappings;

public static class ViewModelMapping
{
    public static AppInitialViewModel ToModel(this InitialSiteDataViewModel vm)
        => new()
        {
            SiteSettings = vm.SiteSettings,
            InitialUserPrimaryInfo = vm.UserPrimaryInfo,
            Options = vm.Options.ToList(),
            NavMenus = vm.NavMenus.ToList(),
            PostTypes = vm.PostTypes.ToList(),
            XActions = vm.XActions,
        };
}
