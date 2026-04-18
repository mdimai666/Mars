using Mars.Core.Exceptions;
using Mars.Host.Shared.Services;
using Mars.SemanticKernel.Host.Shared.Dto;
using Mars.SemanticKernel.Host.Shared.Interfaces;
using Mars.SemanticKernel.Shared.Nodes;
using Microsoft.SemanticKernel;

namespace Mars.SemanticKernel.Host.Service;

internal class MarsAIService : IMarsAIService
{
    private readonly IOptionService _optionService;
    private readonly INodesReader _nodesReader;
    private readonly IAIToolScenarioProvidersLocator _aiToolScenarioProvidersLocator;
    private readonly IKernelFactory _kernelFactory;

    public MarsAIService(IOptionService optionService,
                        INodesReader nodesReader,
                        IAIToolScenarioProvidersLocator aiToolScenarioProvidersLocator,
                        IKernelFactory kernelFactory)
    {
        _optionService = optionService;
        _nodesReader = nodesReader;
        _aiToolScenarioProvidersLocator = aiToolScenarioProvidersLocator;
        _kernelFactory = kernelFactory;
    }

    public async Task<string> Reply(string prompt, string? systemPrompt = null, PromptExecutionSettings? promptExecutionSettings = null, CancellationToken cancellationToken = default)
    {
        //ChatHistory history = [];
        //history.AddSystemMessage(systemPrompt);
        //history.AddSystemMessage(_instructions.ReadBasicConcepts());
        //history.AddUserMessage(prompt);

        var handler = new GeneralAiRequestHandler(_kernelFactory);
        var result = await handler.Handle(prompt, systemPrompt: systemPrompt, promptExecutionSettings: promptExecutionSettings, cancellationToken);
        return result.Content;
    }

    public async Task<string> ReplyAsTool(string prompt, string? toolKeyName = null, bool useToolPresetSettings = true, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(toolKeyName))
        {
            var provider = _aiToolScenarioProvidersLocator.GetProvider(toolKeyName)
                                ?? throw new NotFoundException($"Scenario '{toolKeyName}' not found");

            return await provider.Handle(prompt, cancellationToken).ConfigureAwait(false);
        }

        var configNode = _kernelFactory.GetConfigNode().CopyAsTool();
        var promptExecutionSettings = _kernelFactory.ResolvePromptExecutionSettings(configNode);

        return await Reply(prompt, systemPrompt: configNode.SystemPrompt, promptExecutionSettings: promptExecutionSettings, cancellationToken: cancellationToken);
    }

    public IReadOnlyCollection<string> ToolScenarioList(string[]? tags = null)
    {
        return _aiToolScenarioProvidersLocator.ListProviderKeys(tags);
    }

    public IReadOnlyCollection<AIConfigNodeDto> ConfigList()
    {
        return _nodesReader.Nodes(node => node is SemanticKernelModelConfigNode)
                            .Select(node => node as SemanticKernelModelConfigNode)
                            .Select(node => new AIConfigNodeDto
                            {
                                NodeId = node.Id,
                                Title = node.Label,
                                Description = node.AsDescription()
                            })
                            .ToArray();
    }

}
