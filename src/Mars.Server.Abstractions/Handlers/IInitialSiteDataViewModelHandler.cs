using Mars.Server.Contracts.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Mars.Server.Abstractions.Handlers;

public interface IInitialSiteDataViewModelHandler
{
    Task<InitialSiteDataViewModel> Handle(HttpRequest httpRequest, bool devAdminPageData, CancellationToken cancellationToken);
}
