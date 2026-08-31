using Mars.Admin.Contracts.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Mars.Admin.Host.Handlers;

public interface IInitialSiteDataViewModelHandler
{
    Task<InitialSiteDataViewModel> Handle(HttpRequest httpRequest, bool devAdminPageData, CancellationToken cancellationToken);
}
