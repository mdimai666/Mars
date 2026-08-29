using System.Net.Mime;
using Mars.Server.Abstractions.ExceptionFilters;
using Mars.Server.Abstractions.Handlers;
using Mars.Server.Contracts.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Server.Controllers;

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
    private readonly IInitialSiteDataViewModelHandler _initialSiteDataViewModelHandler;

    public ViewModelController(
        IServiceProvider serviceProvider,
        IInitialSiteDataViewModelHandler initialSiteDataViewModelHandler)
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
