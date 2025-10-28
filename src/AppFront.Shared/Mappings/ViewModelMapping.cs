using AppFront.Shared.Models;
using Mars.Shared.ViewModels;

namespace AppFront.Shared.Mappings;

public static class ViewModelMapping
{
    public static AppInitialViewModel ToModel(this InitialSiteDataViewModel vm)
        => new()
        {
            SysOptions = vm.SysOptions,
            InitailUserPrimaryInfo = vm.UserPrimaryInfo,
            Options = vm.Options.ToList(),
            LocalPages = vm.LocalPages.ToList() ?? [],
            NavMenus = vm.NavMenus.ToList(),
            PostTypes = vm.PostTypes.ToList(),
            XActions = vm.XActions,
        };
}
