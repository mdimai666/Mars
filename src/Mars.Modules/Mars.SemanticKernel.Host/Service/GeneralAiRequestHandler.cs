using Mars.Core.Extensions;
using Mars.SemanticKernel.Host.Shared.Dto;
using Mars.SemanticKernel.Host.Shared.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Mars.SemanticKernel.Host.Service;

internal class GeneralAiRequestHandler
{
    private readonly IKernelFactory _kernelFabric;

    public GeneralAiRequestHandler(IKernelFactory kernelFabric)
    {
        _kernelFabric = kernelFabric;
    }

    public Task<AgentOutput> Handle(string prompt, string? systemPrompt = null, PromptExecutionSettings? promptExecutionSettings = null, CancellationToken cancellationToken = default)
    {
        ChatHistory history = [];
        if (systemPrompt.IsNotNullOrEmpty())
            history.AddSystemMessage(systemPrompt!);
        //history.AddSystemMessage(_instructions.ReadBasicConcepts());
        history.AddUserMessage(prompt);

        return Handle(history, promptExecutionSettings, cancellationToken);
    }

    public async Task<AgentOutput> Handle(ChatHistory chatMessages, PromptExecutionSettings? promptExecutionSettings = null, CancellationToken cancellationToken = default)
    {
        var kernel = _kernelFabric.Create();

        var executionSettings = promptExecutionSettings ?? _kernelFabric.ResolvePromptExecutionSettings();
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var response = await chat.GetChatMessageContentAsync(chatMessages, executionSettings, kernel, cancellationToken: cancellationToken);

        return new() { Content = response.Content! };
    }
}
