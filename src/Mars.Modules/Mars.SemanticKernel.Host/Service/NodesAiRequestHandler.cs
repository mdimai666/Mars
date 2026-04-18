using Mars.Core.Extensions;
using Mars.SemanticKernel.Host.Shared.Dto;
using Mars.SemanticKernel.Host.Shared.Interfaces;
using Mars.SemanticKernel.Shared.Nodes;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Mars.SemanticKernel.Host.Service;

internal class NodesAiRequestHandler
{
    private readonly IKernelFactory _kernelFactory;

    public NodesAiRequestHandler(IKernelFactory kernelFactory)
    {
        _kernelFactory = kernelFactory;
    }

    public async Task<AgentOutput> Handle(string prompt, SemanticKernelModelConfigNode configNode, CancellationToken cancellationToken)
    {
        ChatHistory history = [];
        if (configNode.SystemPrompt.IsNotNullOrEmpty())
            history.AddSystemMessage(configNode.SystemPrompt!);
        //history.AddSystemMessage(_instructions.ReadBasicConcepts());
        history.AddUserMessage(prompt);

        var kernel = _kernelFactory.Create();

        var executionSettings = _kernelFactory.ResolvePromptExecutionSettings(configNode);
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel, cancellationToken: cancellationToken);

        return new() { Content = response.Content! };
    }
}
