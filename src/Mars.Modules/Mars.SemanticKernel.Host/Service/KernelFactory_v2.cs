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
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OllamaSharp;

namespace Mars.SemanticKernel.Host.Service;

//not used. Это raw использование. Сохранено чтобы вспомнить просто.
internal class KernelFactory_v2 : IKernelFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceCollection _services;
    private SemanticKernelModelConfigNode? _configNode;

    private KernelPluginCollection _plugins = new();

    public KernelPluginCollection Plugins => _plugins;

    public KernelFactory_v2(IServiceProvider serviceProvider, IServiceCollection services)
    {
        _serviceProvider = serviceProvider;
        _services = services;
    }

    public static IServiceCollection RegisterKernelFactory(IServiceCollection services)
    {
        services.AddSingleton<IFunctionInvocationFilter, ToolLoggingFilter>();
        services.AddTransient<IKernelFactory, KernelFactory>();

        //services.use

        services.AddTransient<Kernel>((sp) =>
        {
            var factory = new KernelFactory_v2(sp, services);
            return factory.Create();
        });
        services.AddKernel();
        services.AddTransient<KernelPluginCollection>();
        //var kernelBuilder = builder.Services.AddKernel();

        return services;
    }

    public Kernel Create(ILLMOptions? llmOptions = null)
    {
        var kernel = new Kernel(_serviceProvider, _plugins);
        //var toolLoggingFilter = _serviceProvider.GetRequiredService
        //kernel.FunctionInvocationFilters.ad

        var configNode = GetConfigNode();

        llmOptions ??= ResoveNodeConfig(configNode);

        //CreateChatCompletionService()

        //AddChatCompletionService(_services, llmOptions);

        return kernel;
    }

#if false
    public Kernel Create(ILLMOptions? llmOptions = null)
    {
        var kernelBuilder = CreateBuilder(llmOptions);

        return kernelBuilder.Build();
    }

    private IKernelBuilder CreateBuilder(ILLMOptions? llmOptions = null)
    {
        var kernelBuilder = Kernel.CreateBuilder();
        //var kernelBuilder = new MarsKernelBuilder(_services);
        //var k = new Kernel(_serviceProvider, _plugins);
        //kernelBuilder.Services = _services;
        //kernelBuilder.Services.AddSingleton(_services);
        //var kernelBuilder = new KernelBuilder();
        //var kernelBuilder = _services.AddKernel();
        //kernelBuilder.Services.AddSingleton<IFunctionInvocationFilter, ToolLoggingFilter>();

        var configNode = GetConfigNode();

        llmOptions ??= ResoveNodeConfig(configNode);

        AddChatCompletionService(kernelBuilder, llmOptions);

        //kernelBuilder.addol

        // Регистрируем плагины
        //kernelBuilder.Plugins.AddFromType<NewsPlugin>();

        return kernelBuilder;
    } 
#endif

    //private IKernelBuilder CreateBuilderOllama(IServiceProvider services, string modelName, string endpoint)
    //    => CreateBuilder(new OllamaOptions { Endpoint = endpoint, ModelId = modelName });

    public ILLMOptions ResoveNodeConfig(SemanticKernelModelConfigNode configNode)
    {
        return configNode.ModelType switch
        {
            OllamaOptions.SectionName => configNode.ModelConfig.Deserialize<OllamaOptions>()!,
            OpenAIOptions.SectionName => configNode.ModelConfig.Deserialize<OpenAIOptions>()!,
            _ => throw new UserActionException($"Unknown model type '{configNode.ModelType}'")
        };
    }

#if false
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
#endif

    private static IServiceCollection AddChatCompletionService(IServiceCollection services, ILLMOptions llmOptions)
    {
        if (llmOptions is OllamaOptions ollamaOptions)
        {
            services.AddOllamaChatCompletion(
                modelId: ollamaOptions.ModelId,
                endpoint: new Uri(ollamaOptions.Endpoint));
        }
        else if (llmOptions is OpenAIOptions openAIOptions)
        {
            services.AddOpenAIChatCompletion(
                modelId: openAIOptions.ModelId,
                endpoint: new Uri(openAIOptions.Endpoint.IsNotNullOrEmpty() ? openAIOptions.Endpoint : OpenAIOptions.DefaultEndpoint),
                apiKey: openAIOptions.ApiKey,
                orgId: openAIOptions.OrgId);
        }
        else
        {
            throw new ArgumentException($"Unsupported LLM options '{llmOptions.GetType().Name}'");
        }
        return services;
    }

    public IChatCompletionService CreateChatCompletionService()
        => CreateChatCompletionService(null);

    public IChatCompletionService CreateChatCompletionService(ILLMOptions? llmOptions = null)
    {
        var configNode = GetConfigNode();
        llmOptions ??= ResoveNodeConfig(configNode);

        IChatClient chatClient = llmOptions switch
        {
            OllamaOptions ollamaOptions => new OllamaApiClient(
                uriString: ollamaOptions.Endpoint,
                defaultModel: ollamaOptions.ModelId),
            OpenAIOptions openAIOptions => new OpenAIChatCompletionService(
                modelId: openAIOptions.ModelId,
                endpoint: new Uri(openAIOptions.Endpoint.IsNotNullOrEmpty() ? openAIOptions.Endpoint : OpenAIOptions.DefaultEndpoint),
                apiKey: openAIOptions.ApiKey,
                organization: openAIOptions.OrgId).AsChatClient(),
            _ => throw new NotImplementedException($"Unknown model option type '{llmOptions.GetType().Name}'")
        };

        //chatCompletionService

        //var httpClient ??= HttpClientProvider.GetHttpClient(httpClient, serviceProvider);

        var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();

        //var ollamaClient = (IChatClient)new OllamaApiClient(httpClient, modelId);

        var builder = chatClient.AsBuilder();
        if (loggerFactory is not null)
        {
            builder.UseLogging(loggerFactory);
        }

        return builder
            .UseKernelFunctionInvocation(loggerFactory)
            .Build(_serviceProvider)
            .AsChatCompletionService();
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
