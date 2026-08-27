using Mars.Server.Abstractions.Services;
using Mars.SemanticKernel.Abstractions.Interfaces;
using Mars.Contracts.Exceptions;

namespace Mars.Services;

internal class AIToolService : IAIToolService
{
    private readonly IMarsAIService? _marsAIService;

    public AIToolService(IMarsAIService? marsAIService)
    {
        _marsAIService = marsAIService;
    }

    public async Task<string> Prompt(string prompt, CancellationToken cancellationToken)
    {
        if (_marsAIService is null)
            throw new AIServiceNotAvailableException("AI service is not available.");

        var response = await _marsAIService.Reply(prompt, systemPrompt: null, cancellationToken: cancellationToken);

        return response.ToString();
    }
}
