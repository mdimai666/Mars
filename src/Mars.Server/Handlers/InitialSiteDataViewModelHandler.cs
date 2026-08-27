using Mars.Cms.Abstractions.Mappings.NavMenus;
using Mars.Cms.Abstractions.Mappings.PostTypes;
using Mars.Cms.Abstractions.Services;
using Mars.Contracts.ViewModels;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Identity.Abstractions.Mappings.Users;
using Mars.Options.Mappings.Options;
using Mars.Options.Services;
using Mars.Server.Abstractions.Managers;
using Microsoft.AspNetCore.Http;

namespace Mars.Server.Handlers;

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
