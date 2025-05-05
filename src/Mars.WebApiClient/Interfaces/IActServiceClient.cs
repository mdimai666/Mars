using Mars.Shared.Contracts.XActions;

namespace Mars.WebApiClient.Interfaces;

public interface IActServiceClient
{
    Task<XActResult> Inject(string actionId, string[] args);
}
