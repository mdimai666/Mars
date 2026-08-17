using System.Net.Mime;
using Mars.Host.Handlers;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Shared.ViewModels;
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

    public ViewModelController(
        IServiceProvider serviceProvider,
        InitialSiteDataViewModelHandler initialSiteDataViewModelHandler)
    {
        _serviceProvider = serviceProvider;
        _initialSiteDataViewModelHandler = initialSiteDataViewModelHandler;
    }

    [HttpGet]
    public Task<InitialSiteDataViewModel> InitialSiteDataViewModel(bool devAdminPageData = false, CancellationToken cancellationToken = default)
    {
        return _initialSiteDataViewModelHandler.Handle(Request, devAdminPageData, cancellationToken);
        //return await InitialSiteDataViewModel(_serviceProvider, Request, devAdminPageData: devAdminPageData);
    }
}
