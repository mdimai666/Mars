using System.Text.Json;
using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Host.Shared.Services;
using Mars.SemanticKernel.Host.Plugins;
using Mars.SemanticKernel.Host.Shared.Interfaces;
using Mars.SemanticKernel.Shared.Nodes;
using Mars.SemanticKernel.Shared.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Mars.SemanticKernel.Host.Service;

internal class KernelFactory : IKernelFactory
{
    private readonly IServiceProvider _serviceProvider;
    private SemanticKernelModelConfigNode? _configNode;
    private Kernel? _kernel;

    public KernelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static IServiceCollection RegisterKernelFabric(IServiceCollection services)
    {
        services.AddSingleton<IFunctionInvocationFilter, ToolLoggingFilter>();
        services.AddTransient<IKernelFactory, KernelFactory>();

        services.AddTransient<Kernel>((sp) =>
        {
            var fabric = new KernelFactory(sp);
            return fabric.Create();
        });
        return services;
    }

    public Kernel Create(ILLMOptions? llmOptions = null)
    {
        var kernelBuilder = CreateBuilder(llmOptions);

        return _kernel = kernelBuilder.Build();
    }

    private IKernelBuilder CreateBuilder(ILLMOptions? llmOptions = null)
    {
        var kernelBuilder = Kernel.CreateBuilder();

        foreach (var filter in _serviceProvider.GetService<IEnumerable<IFunctionInvocationFilter>>() ?? [])
        {
            kernelBuilder.Services.AddSingleton(typeof(IFunctionInvocationFilter), filter);
        }

        var configNode = GetConfigNode();
        llmOptions ??= ResoveNodeConfig(configNode);

        AddChatCompletionService(kernelBuilder, llmOptions);

        return kernelBuilder;
    }

    private IKernelBuilder AddChatCompletionService(IKernelBuilder kernelBuilder, ILLMOptions llmOptions)
    {
        if (llmOptions is OllamaOptions ollamaOptions)
        {
            kernelBuilder.AddOllamaChatCompletion(
                modelId: ollamaOptions.ModelId,
                endpoint: new Uri(ollamaOptions.Endpoint));
        }
        else if (llmOptions is OpenAIOptions openAIOptions)
        {
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: openAIOptions.ModelId,
                endpoint: new Uri(openAIOptions.Endpoint.IsNotNullOrEmpty() ? openAIOptions.Endpoint : OpenAIOptions.DefaultEndpoint),
                apiKey: openAIOptions.ApiKey,
                orgId: openAIOptions.OrgId);
        }
        else
        {
            throw new ArgumentException($"Unsupported LLM options '{llmOptions.GetType().Name}'");
        }
        return kernelBuilder;
    }

    public IChatCompletionService CreateChatCompletionService()
    {
        return _kernel.Services.GetRequiredService<IChatCompletionService>();
    }

    public ILLMOptions ResoveNodeConfig(SemanticKernelModelConfigNode configNode)
    {
        return configNode.ModelType switch
        {
            OllamaOptions.SectionName => configNode.ModelConfig.Deserialize<OllamaOptions>()!,
            OpenAIOptions.SectionName => configNode.ModelConfig.Deserialize<OpenAIOptions>()!,
            _ => throw new UserActionException($"Unknown model type '{configNode.ModelType}'")
        };
    }

    public PromptExecutionSettings ResolvePromptExecutionSettings()
        => ResolvePromptExecutionSettings(GetConfigNode());

    public PromptExecutionSettings ResolvePromptExecutionSettings(SemanticKernelModelConfigNode configNode)
    {
        return configNode.ModelType switch
        {
            OllamaOptions.SectionName => new OllamaPromptExecutionSettings
            {
                //FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                FunctionChoiceBehavior = FunctionChoiceBehavior.Required(),
                Temperature = configNode.Temperature,
                TopK = configNode.TopK,
                TopP = configNode.TopP,
                ExtensionData = new Dictionary<string, object>
                {
                    //{ "reasoning", "none" } // Установите "low", "medium" или "high"
                    //["reasoning_effort"] = "low",
                    //["thinking_level"] = "low",
                    //["num_predict "] = 64,
                    //["think"] = "low",
                    //["reasoning_effort"] = "low",
                },
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

    private SemanticKernelModelConfigNode ReadFromOptions(IOptionService optionService, INodesReader nodesReader)
    {
        var aiToolOption = optionService.GetOption<AIToolOption>();

        if (string.IsNullOrEmpty(aiToolOption.DefaultAIToolConfig)) throw new UserActionException("AITool not configured");
        var configNode = nodesReader.GetNode(aiToolOption.DefaultAIToolConfig) as SemanticKernelModelConfigNode;
        if (configNode is null) throw new UserActionException($"SemanticKernelModelConfigNode not with id '{configNode.Id}' not found");
        return configNode;
    }

    public SemanticKernelModelConfigNode GetConfigNode()
    {
        return _configNode ??= ReadFromOptions(_serviceProvider.GetRequiredService<IOptionService>(), _serviceProvider.GetRequiredService<INodesReader>());
    }
}
