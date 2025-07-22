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

    /// <summary>
    /// reply as minimal creative and predictable. Using: SemanticKernelModelConfigNode.SetAsToolParams()
    /// Can use context registered in <see cref="IAIToolScenarioProvidersLocator"/>
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="toolKeyName"><see cref="IAIToolScenarioProvider"/></param>
    /// <param name="useToolPresetSettings"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<string> ReplyAsTool(string prompt, string? toolKeyName = null, bool useToolPresetSettings = true, CancellationToken cancellationToken = default);
    IReadOnlyCollection<string> ToolScenarioList(string[]? tags = null);
}
