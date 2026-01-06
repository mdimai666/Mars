using System.Net.Mime;
using Mars.Core.Exceptions;
using Mars.Handlers;
using Mars.Host.Handlers;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Mappings.NavMenus;
using Mars.Host.Shared.Mappings.Options;
using Mars.Host.Shared.Mappings.PostTypes;
using Mars.Host.Shared.Mappings.Renders;
using Mars.Host.Shared.Mappings.Search;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.Search;
using Mars.Shared.Models;
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
    private readonly InitialSiteDataViewModelHandler _initialSiteDataViewModelHandler;
    private readonly PostTypePresentationRenderHandler _postTypePresentationRenderHandler;
    private readonly ICentralSearchService _centralSearchService;

    public ViewModelController(
        IServiceProvider serviceProvider,
        InitialSiteDataViewModelHandler initialSiteDataViewModelHandler,
        PostTypePresentationRenderHandler postTypePresentationRenderHandler,
        ICentralSearchService centralSearchService)
    {
        _serviceProvider = serviceProvider;
        _initialSiteDataViewModelHandler = initialSiteDataViewModelHandler;
        _postTypePresentationRenderHandler = postTypePresentationRenderHandler;
        _centralSearchService = centralSearchService;
    }

    [HttpGet]
    public Task<InitialSiteDataViewModel> InitialSiteDataViewModel(bool devAdminPageData = false, CancellationToken cancellationToken = default)
    {
        return _initialSiteDataViewModelHandler.Handle(Request, devAdminPageData, cancellationToken);
        //return await InitialSiteDataViewModel(_serviceProvider, Request, devAdminPageData: devAdminPageData);
    }

    [HttpGet]
    [Authorize]
    public async Task<IReadOnlyCollection<SearchFoundElementResponse>> GlobalSearch(string text, int maxCount = 10, CancellationToken cancellationToken = default)
    {
        if (maxCount > 30) throw MarsValidationException.FromSingleError(nameof(maxCount), "maxCount maximum is 30");
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 2) return [];
        var results = await _centralSearchService.ActionBarSearch(text, maxCount, cancellationToken);
        return results.ToResponse();
    }

    [HttpGet]
    [Authorize]
    [Produces(MediaTypeNames.Text.Html)]
    public async Task<IActionResult> PostTypePresentationRender(SourceUri sourceUri, string? queryString, CancellationToken cancellationToken)
    {
        var html = (await _postTypePresentationRenderHandler.Handle(sourceUri, queryString, HttpContext, cancellationToken)) ?? throw new NotFoundException();
        return Content(html);
    }
}
