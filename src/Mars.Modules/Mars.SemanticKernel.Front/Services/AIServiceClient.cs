using Flurl.Http;
using Mars.SemanticKernel.Shared.Contracts;

namespace Mars.SemanticKernel.Front.Services;

internal class AIServiceClient : IAIServiceClient
{
    protected readonly IFlurlClient _client;
    protected string _basePath;
    protected string _controllerName;

    public AIServiceClient(IFlurlClient client)
    {
        _basePath = "/api/";
        _controllerName = "AITool";
        _client = client;
    }

    public Task<AIServiceResponse> Prompt(AIServiceRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "Prompt")
                    .PostJsonAsync(request)
                    .ReceiveJson<AIServiceResponse>();

    public Task<IReadOnlyCollection<AIConfigNodeResponse>> ConfigList()
        => _client.Request($"{_basePath}{_controllerName}", "ConfigList")
                    .GetJsonAsync<IReadOnlyCollection<AIConfigNodeResponse>>();
}
