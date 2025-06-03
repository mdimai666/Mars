using Mars.SemanticKernel.Shared.Contracts;
using Mars.WebApiClient.Interfaces;

namespace Mars.SemanticKernel.Front.Services;

public interface IAIServiceClient
{
    Task<AIServiceResponse> Prompt(AIServiceRequest request);
    Task<IReadOnlyCollection<AIConfigNodeResponse>> ConfigList();
}

public static class WebApiClientAIServiceClientExtensions
{
    public static IAIServiceClient Docker(this IMarsWebApiClient client)
    {
        return new AIServiceClient(client.Client);
    }
}
