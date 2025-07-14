using System.Text.Json;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Services;
using Mars.SemanticKernel.Host.Shared.Dto;
using Mars.SemanticKernel.Host.Shared.Interfaces;
using Mars.SemanticKernel.Shared.Nodes;
using Mars.SemanticKernel.Shared.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OllamaSharp;

namespace Mars.SemanticKernel.Host.Service;

internal class MarsAIService : IMarsAIService
{
    private readonly IOptionService _optionService;
    private readonly INodesReader _nodesReader;

    public MarsAIService(IOptionService optionService, INodesReader nodesReader)
    {
        _optionService = optionService;
        _nodesReader = nodesReader;
    }

    public Task<string> Reply(string prompt, string? systemPrompt, PromptExecutionSettings? promptExecutionSettings = null, CancellationToken cancellationToken = default)
    {
        var aiToolOption = _optionService.GetOption<AIToolOption>();

        if (string.IsNullOrEmpty(aiToolOption.DefaultAIToolConfig)) throw new UserActionException("AITool not configured");
        var configNode = _nodesReader.GetNode(aiToolOption.DefaultAIToolConfig) as SemanticKernelModelConfigNode;
        if (configNode is null) throw new UserActionException($"SemanticKernelModelConfigNode not with id '{configNode.Id}' not found");

        return Reply(prompt,
                    systemPrompt,
                    configNode,
                    promptExecutionSettings: promptExecutionSettings,
                    cancellationToken: cancellationToken);
    }

    public async Task<string> Reply(string prompt,
                                    string? systemPrompt,
                                    SemanticKernelModelConfigNode configNode,
                                    PromptExecutionSettings? promptExecutionSettings = null,
                                    CancellationToken cancellationToken = default)
    {
        var aiTool = ResolveAITool(configNode, promptExecutionSettings);

        var chatHistory = string.IsNullOrEmpty(systemPrompt) ? [] : new ChatHistory(systemPrompt);

        chatHistory.AddUserMessage(prompt);

        // assistant message
        var reply = await aiTool.ChatCompletionService.GetChatMessageContentAsync(chatHistory, aiTool.PromptExecutionSettings, cancellationToken: cancellationToken);

        chatHistory.Add(reply);

        return reply.Content!;
    }

    internal AIToolInfo ResolveAITool(SemanticKernelModelConfigNode configNode, PromptExecutionSettings? promptExecutionSettings)
    {
        ILLMOptions llmOption = configNode.ModelType switch
        {
            OllamaOptions.SectionName => configNode.ModelConfig.Deserialize<OllamaOptions>()!,
            OpenAIOptions.SectionName => configNode.ModelConfig.Deserialize<OpenAIOptions>()!,
            _ => throw new UserActionException($"Unknown model type '{configNode.ModelType}'")
        };

        var executionSettings = promptExecutionSettings ?? ResolvePromptExecutionSettings(configNode);

        IChatCompletionService chatCompletionService = llmOption switch
        {
            OllamaOptions ollamaOptions => new OllamaApiClient(
                uriString: ollamaOptions.Endpoint,
                defaultModel: ollamaOptions.ModelId).AsChatCompletionService(),
            OpenAIOptions openAIOptions => new OpenAIChatCompletionService(
                openAIOptions.ModelId,
                openAIOptions.ApiKey,
                openAIOptions.OrgId),
            _ => throw new UserActionException($"Unknown model type '{configNode.ModelType}'")
        };

        return new AIToolInfo
        {
            OptionType = llmOption.GetType(),
            lLMOptions = llmOption,
            ChatCompletionService = chatCompletionService,
            PromptExecutionSettings = executionSettings
        };
    }

    PromptExecutionSettings ResolvePromptExecutionSettings(SemanticKernelModelConfigNode configNode)
    {
        return configNode.ModelType switch
        {
            OllamaOptions.SectionName => new OllamaPromptExecutionSettings
            {
                Temperature = configNode.Temperature,
                TopK = configNode.TopK,
                TopP = configNode.TopP,
            },
            OpenAIOptions.SectionName => new OpenAIPromptExecutionSettings
            {
                Temperature = configNode.Temperature,
                //TopK = node.TopK,
                TopP = configNode.TopP,
            },
            _ => throw new UserActionException($"Unknown model type '{configNode.ModelType}'")
        };
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

internal class AIToolInfo
{
    public required Type OptionType { get; init; }
    public required ILLMOptions lLMOptions { get; init; }
    public required IChatCompletionService ChatCompletionService { get; init; }
    public required PromptExecutionSettings PromptExecutionSettings { get; init; }
}
