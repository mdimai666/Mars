using Mars.Host.Shared.Dto.Renders;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Mappings.NavMenus;
using Mars.Host.Shared.Mappings.Options;
using Mars.Host.Shared.Mappings.PostTypes;
using Mars.Host.Shared.Mappings.Renders;
using Mars.Host.Shared.Mappings.Users;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Renders;
using Mars.Shared.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Mars.Host.Handlers;

public class InitialSiteDataViewModelHandler(IOptionService optionService,
                                            INavMenuService navMenuService,
                                            IPageRenderService pageRenderService,
                                            IMetaModelTypesLocator metaModelTypesLocator,
                                            IRequestContext requestContext,
                                            IActionManager actionManager)
{
    public async Task<InitialSiteDataViewModel> Handle(HttpRequest httpRequest, bool devAdminPageData, CancellationToken cancellationToken)
    {
        var req = new WebClientRequest(httpRequest);
        var httpContext = httpRequest.HttpContext;
        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;

        var menus = navMenuService.GetAppInitialDataMenus(devAdminPageData);

        RenderActionResult<PostRenderDto>? indexPage = null;

        //indexPage = await renderService.RenderUrl("/", httpContext, renderParam: new Shared.WebSite.Models.RenderParam { AllowLayout = true, OnlyBody = true }, cancellationToken);

        RenderActionResult<PostRenderDto>? currentPage = null;

        if (af is not null
            && af.Configuration.Mode == Core.Models.AppFrontMode.BlazorPrerender
            && req.Path != "/" && !req.Path.StartsWithSegments("/dev")
            && !req.Path.StartsWithSegments("/AppAdmin"))
        {
            //var exp = renderService.ResolveUrlGetPostExpr(req.Path.Value);
            //if (exp != null)
            //{
            //    currentPage = await postService.RenderEx(exp, req);
            //}

            var currentPageRendered = await pageRenderService.RenderUrl(httpRequest.Path, httpContext, cancellationToken: cancellationToken);
            if (currentPageRendered is not null)
            {
                currentPage = currentPageRendered;
            }
        }

        var localPages = new List<RenderActionResult<PostRenderResponse>>();
        if (indexPage is not null) localPages.Add(indexPage.ToResponse());
        if (currentPage is not null) localPages.Add(currentPage.ToResponse());

        var options = optionService.GetOptionsForInitialSiteData();

        if (options.Count > 50) throw new Exception("too much from options");

        var postTypes = metaModelTypesLocator.PostTypesDict().Values.Select(PostTypeMapping.ToAdminPanelItemResponse).ToList();

        var userPrimaryInfo = requestContext.User!?.ToPrimaryInfo();

        return new InitialSiteDataViewModel
        {
            SysOptions = optionService.SysOption,
            UserPrimaryInfo = userPrimaryInfo,
            PostTypes = postTypes,
            NavMenus = menus.Select(NavMenuMapping.ToResponse).ToList(),
            LocalPages = localPages ?? [],
            Options = options.Select(OptionMapping.ToResponse).ToList(),
            XActions = actionManager.XActions,
        };
    }
}
