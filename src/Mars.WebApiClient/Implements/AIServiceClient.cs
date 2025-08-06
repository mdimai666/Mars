using Flurl.Http;
using Mars.Shared.Contracts.AIService;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class AIServiceClient : BasicServiceClient, IAIServiceClient
{
    public AIServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "AITool";
    }

    public Task<AIServiceResponse> Prompt(AIServiceRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "Prompt")
                    .PostJsonAsync(request)
                    .ReceiveJson<AIServiceResponse>();

    public Task<IReadOnlyCollection<AIConfigNodeResponse>> ConfigList()
        => _client.Request($"{_basePath}{_controllerName}", "ConfigList")
                    .GetJsonAsync<IReadOnlyCollection<AIConfigNodeResponse>>();

    public Task<IReadOnlyCollection<string>> ToolScenarioList()
        => _client.Request($"{_basePath}{_controllerName}", "ToolScenarioList")
                    .GetJsonAsync<IReadOnlyCollection<string>>();

    public Task<AIServiceResponse> ToolPrompt(AIServiceToolRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "ToolPrompt")
                    .OnError(OnStatus404ThrowException)
                    .PostJsonAsync(request)
                    .ReceiveJson<AIServiceResponse>();

}
