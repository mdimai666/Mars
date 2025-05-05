using System.Net.Mime;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Options;
using Mars.Host.Shared.Dto.Renders;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Mappings.NavMenus;
using Mars.Host.Shared.Mappings.Options;
using Mars.Host.Shared.Mappings.PostTypes;
using Mars.Host.Shared.Mappings.Renders;
using Mars.Host.Shared.Mappings.Search;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Renders;
using Mars.Shared.Contracts.Search;
using Mars.Shared.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

[ApiController]
[Route("vm/[controller]/[action]")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class ViewModelController : ControllerBase //MinimalControllerBase, IViewModelService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRequestContext _requestContext;
    private readonly IActionManager _actionManager;
    private readonly ICentralSearchService _centralSearchService;

    public ViewModelController(
        IServiceProvider serviceProvider,
        IRequestContext requestContext,
        IActionManager actionManager,
        ICentralSearchService centralSearchService)
    {
        _serviceProvider = serviceProvider;
        _requestContext = requestContext;
        _actionManager = actionManager;
        _centralSearchService = centralSearchService;
    }


    [HttpGet]
    public async Task<InitialSiteDataViewModel> InitialSiteDataViewModel(bool devAdminPageData = false)
    {
        return await InitialSiteDataViewModel(_serviceProvider, Request, devAdminPageData: devAdminPageData);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public DevAdminExtraViewModel DevAdminExtraViewModel()
    {
        //var ef = serviceProvider.GetRequiredService<MarsDbContext>();

        return new DevAdminExtraViewModel
        {
            XActions = _actionManager.XActions.ToDictionary()
        };
    }

    public static async Task<InitialSiteDataViewModel> InitialSiteDataViewModel(IServiceProvider serviceProvider, HttpRequest request, bool devAdminPageData = false, CancellationToken cancellationToken = default)
    {
        var renderService = serviceProvider.GetRequiredService<IPageRenderService>();
        var optionService = serviceProvider.GetRequiredService<IOptionService>();
        var navMenuService = serviceProvider.GetRequiredService<INavMenuService>();

        var menus = navMenuService.GetAppInitialDataMenus(devAdminPageData);

        var req = new WebClientRequest(request);

        var httpContext = serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;

        var postTypeRepository = serviceProvider.GetRequiredService<IPostTypeRepository>();

        RenderActionResult<PostRenderDto>? indexPage = null;

        //indexPage = await renderService.RenderUrl("/", httpContext, renderParam: new Shared.WebSite.Models.RenderParam { AllowLayout = true, OnlyBody = true }, cancellationToken);

        RenderActionResult<PostRenderDto>? currentPage = null;

        if (af.Configuration.Mode == Core.Models.AppFrontMode.BlazorPrerender && req.Path != "/" && !req.Path.StartsWithSegments("/dev") && !req.Path.StartsWithSegments("/AppAdmin"))
        {
            //var exp = renderService.ResolveUrlGetPostExpr(req.Path.Value);
            //if (exp != null)
            //{
            //    currentPage = await postService.RenderEx(exp, req);
            //}

            var currentPageRendered = await renderService.RenderUrl(request.Path, httpContext, cancellationToken: cancellationToken);
            if (currentPageRendered is not null)
            {
                currentPage = currentPageRendered;
            }
        }

        var localPages = new List<RenderActionResult<PostRenderResponse?>>();
        if (indexPage is not null) localPages.Add(indexPage.ToResponse());
        if (currentPage is not null) localPages.Add(currentPage.ToResponse());

        var options = optionService.GetOptionsForInitialSiteData();

        if (options.Count > 30) throw new Exception("too much from options");

        return new InitialSiteDataViewModel
        {
            SysOptions = optionService.SysOption,
            PostTypes = (await postTypeRepository.ListAll(default)).Select(s => s.ToResponse()).ToList(),
            NavMenus = menus.Select(NavMenuMapping.ToResponse).ToList(),
            LocalPages = localPages,
            Options = options.Select(OptionMapping.ToResponse).ToList(),
        };
    }

    [Authorize(Roles = "Admin")]
    //[AllowAnonymous]
    [HttpGet]
    public Task<IActionResult> SystemExportSettings()
    {
        //var ef = GetEFContext();


        //SystemImportSettingsFile_v1 export = new SystemImportSettingsFile_v1
        //{
        //    SysOptions = _optionService.SysOption,
        //    SmtpSettings = _optionService.MailSettings,
        //    //FaqPosts = ef.FaqPosts.ToList(),
        //    Roles = ef.Roles.ToList(),
        //    ContactPersons = ef.ContactPeople.ToList(),
        //};

        //byte[] SerializeObject(object value) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));


        //return File(SerializeObject(export), "application/json", "site-export-settings.json");

        throw new NotImplementedException();
    }

    [Authorize(Roles = "Admin")]
    //[AllowAnonymous]
    [HttpPost]
    public Task<ActionResult<UserActionResult>> SystemImportSettings(SystemImportSettingsFile_v1 import)
    {
        //try
        //{
        //    _optionService.SaveOption(import.SysOptions);

        //    _optionService.SaveSmtpSettings(import.SmtpSettings);

        //    var ef = GetEFContext();

        //    //if (import.FaqPosts != null)
        //    //{
        //    //    ef.AttachRange(import.FaqPosts);
        //    //    foreach (var e in import.FaqPosts)
        //    //        ef.Entry(e).State = EntityState.Modified;
        //    //}
        //    if (import.ContactPersons != null)
        //    {
        //        ef.AttachRange(import.ContactPersons);
        //        foreach (var e in import.ContactPersons)
        //            ef.Entry(e).State = EntityState.Modified;
        //    }

        //    ef.SaveChanges();

        //    return new UserActionResult
        //    {
        //        Ok = true,
        //        Message = "Успешно импортировано"
        //    };

        //}
        //catch (Exception ex)
        //{
        //    return new UserActionResult
        //    {
        //        Message = ex.Message,
        //    };
        //}
        throw new NotImplementedException();
    }

    [HttpGet]
    [Authorize]
    public async Task<IReadOnlyCollection<SearchFoundElementResponse>> GlobalSearch(string text, int maxCount = 10, CancellationToken cancellationToken = default)
    {
        if (maxCount > 30) throw new MarsValidationException(new Dictionary<string, string[]> { [nameof(maxCount)] = ["maxCount maximum is 30"] });
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 2) return [];
        var results = await _centralSearchService.ActionBarSearch(text, maxCount, cancellationToken);
        return results.ToResponse();
    }

    #region NOT_USED
    //[HttpGet("{name}")]
    //public ActionResult Get([FromServices] IServiceProvider serviceProvider, [FromServices] ViewModelService viewModelService, string name)
    //{
    //    var r = Request;

    //    var result = viewModelService.GetViewModelByName(name);

    //    if (result == null)
    //    {
    //        return NotFound();
    //    }

    //    return Ok(result);
    //}

    //[HttpGet("Test")]
    #endregion
}
