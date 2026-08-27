using Mars.SemanticKernel.Shared.Nodes;
using Mars.SemanticKernel.Shared.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Mars.SemanticKernel.Host.Shared.Interfaces;

public interface IKernelFactory
{
    Kernel Create(ILLMOptions? llmOptions = null);
    PromptExecutionSettings ResolvePromptExecutionSettings();
    PromptExecutionSettings ResolvePromptExecutionSettings(SemanticKernelModelConfigNode configNode);
    SemanticKernelModelConfigNode GetConfigNode();
    IChatCompletionService CreateChatCompletionService();
}
