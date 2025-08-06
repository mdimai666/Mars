using Mars.Shared.Contracts.AIService;

namespace Mars.WebApiClient.Interfaces;

public interface IAIServiceClient
{
    Task<AIServiceResponse> Prompt(AIServiceRequest request);
    Task<IReadOnlyCollection<AIConfigNodeResponse>> ConfigList();
    Task<AIServiceResponse> ToolPrompt(AIServiceToolRequest request);
    Task<IReadOnlyCollection<string>> ToolScenarioList();
}
