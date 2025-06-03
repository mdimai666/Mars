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

        var modelOptions = configNode.ModelConfig.Deserialize<OllamaOptions>();

        using var ollamaClient = new OllamaApiClient(
                                       uriString: modelOptions.Endpoint,
                                       defaultModel: modelOptions.ModelId);

        var executionSettings = promptExecutionSettings ?? ResolvePromptExecutionSettings(configNode);

        var chatService = ollamaClient.AsChatCompletionService();

        var chatHistory = string.IsNullOrEmpty(systemPrompt) ? new() : new ChatHistory(systemPrompt);

        chatHistory.AddUserMessage(prompt);

        // assistant message
        var reply = await chatService.GetChatMessageContentAsync(chatHistory, executionSettings, cancellationToken: cancellationToken);

        chatHistory.Add(reply);

        return reply.Content!;
    }

    //IChatCompletionService ResolveChatCompletionService(string configNodeId = "")
    //{
    //    var ollamaClient = new OllamaApiClient(
    //                                   uriString: modelOptions.Endpoint,
    //                                   defaultModel: modelOptions.ModelId);
    //    var chatService = ollamaClient.AsChatCompletionService();

    //    return chatService;
    //}

    PromptExecutionSettings ResolvePromptExecutionSettings(SemanticKernelModelConfigNode node)
    {
        var executionSettings = new OllamaPromptExecutionSettings
        {
            Temperature = node.Temperature,
            TopK = node.TopK,
            TopP = node.TopP,
        };
        return executionSettings;
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
