using Mars.SemanticKernel.Contracts.Nodes;
using Mars.SemanticKernel.Contracts.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Mars.SemanticKernel.Abstractions.Interfaces;

public interface IKernelFactory
{
    Kernel Create(ILLMOptions? llmOptions = null);
    PromptExecutionSettings ResolvePromptExecutionSettings();
    PromptExecutionSettings ResolvePromptExecutionSettings(SemanticKernelModelConfigNode configNode);
    SemanticKernelModelConfigNode GetConfigNode();
    IChatCompletionService CreateChatCompletionService();
}
