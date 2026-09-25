using System.Net.Mime;
using Mars.Admin.Contracts.ViewModels;
using Mars.Admin.Host.Handlers;
using Mars.Server.Abstractions.ExceptionFilters;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Admin.Host.Controllers;

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

    /// <summary>
    /// Сознательно без [Authorize]: отдаёт тот же набор данных, что вшивается в хост-страницу
    /// _AdminHost.cshtml (она рендерится и анонимно — для страницы логина). UserPrimaryInfo
    /// берётся из ambient-аутентификации: аноним → null. Используется WASM-админкой как
    /// remote-fallback начальных данных.
    /// </summary>
    [HttpGet]
    public Task<InitialSiteDataViewModel> InitialSiteDataViewModel(bool devAdminPageData = false, CancellationToken cancellationToken = default)
    {
        return _initialSiteDataViewModelHandler.Handle(Request, devAdminPageData, cancellationToken);
    }
}
