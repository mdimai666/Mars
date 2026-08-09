using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Mappings.NavMenus;
using Mars.Host.Shared.Mappings.Options;
using Mars.Host.Shared.Mappings.PostTypes;
using Mars.Host.Shared.Mappings.Users;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Mars.Host.Handlers;

public class InitialSiteDataViewModelHandler(IOptionService optionService,
                                            INavMenuService navMenuService,
                                            IMetaModelTypesLocator metaModelTypesLocator,
                                            IRequestContext requestContext,
                                            IActionManager actionManager)
{
    public Task<InitialSiteDataViewModel> Handle(HttpRequest httpRequest, bool devAdminPageData, CancellationToken cancellationToken)
    {
        var menus = navMenuService.GetAppInitialDataMenus(devAdminPageData);

        var options = optionService.GetOptionsForInitialSiteData();

        if (options.Count > 50) throw new Exception("too much from options");

        var postTypes = metaModelTypesLocator.PostTypesDict().Values.Select(PostTypeMapping.ToAdminPanelItemResponse).ToList();

        var userPrimaryInfo = requestContext.User!?.ToPrimaryInfo();

        return Task.FromResult(new InitialSiteDataViewModel
        {
            SysOptions = optionService.SysOption,
            UserPrimaryInfo = userPrimaryInfo,
            PostTypes = postTypes,
            NavMenus = menus.Select(NavMenuMapping.ToResponse).ToList(),
            Options = options.Select(OptionMapping.ToResponse).ToList(),
            XActions = actionManager.XActions,
        });
    }
}
