using Mars.SemanticKernel.Host.Shared.Dto;
using Mars.SemanticKernel.Shared.Nodes;
using Microsoft.SemanticKernel;

namespace Mars.SemanticKernel.Host.Shared.Interfaces;

public interface IMarsAIService
{
    IReadOnlyCollection<AIConfigNodeDto> ConfigList();
    Task<string> Reply(string prompt, string? systemPrompt = null, PromptExecutionSettings? promptExecutionSettings = null,
        CancellationToken cancellationToken = default);
    Task<string> Reply(string prompt, string? systemPrompt, SemanticKernelModelConfigNode configNode, PromptExecutionSettings? promptExecutionSettings = null,
        CancellationToken cancellationToken = default);
}
