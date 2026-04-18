using System.Text.Json;
using AutoFixture;
using Mars.Host.Shared.Services;
using Mars.SemanticKernel.Host.Service;
using Mars.SemanticKernel.Host.Shared.Interfaces;
using Mars.SemanticKernel.Shared.Nodes;
using Mars.SemanticKernel.Shared.Options;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.AiServices.Integration.Tests.Common;

public abstract class ScenarioTestBase
{
    public readonly IFixture _fixture = new Fixture();
    private readonly IServiceProvider _serviceProvider;
    public readonly IMarsAIService AIService;
    public readonly IAIToolService AIToolService;

    public abstract string SystemPrompt { get; }

    public ScenarioTestBase()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();

        var modelConfig = new OllamaOptions()
        {
            //ModelId = "gpt-oss:20b"
            ModelId = "gemma4:e4b"
        };
        var configNode = new SemanticKernelModelConfigNode()
        {
            Id = new Guid("2972093f-5121-4d7a-ac08-90c4b7705774").ToString(),
            ModelConfig = JsonSerializer.SerializeToNode(modelConfig)!,
        };
        var aiToolOption = new AIToolOption()
        {
            DefaultAIToolConfig = configNode.Id
        };
        var optionService = Substitute.For<IOptionService>();
        optionService.GetOption<AIToolOption>().Returns(aiToolOption);
        var nodesReader = Substitute.For<INodesReader>();
        nodesReader.GetNode(aiToolOption.DefaultAIToolConfig).Returns(configNode);
        var aiToolScenarioProvidersLocator = Substitute.For<IAIToolScenarioProvidersLocator>();

        _serviceProvider.GetService(typeof(IOptionService)).Returns(optionService);
        _serviceProvider.GetService(typeof(INodesReader)).Returns(nodesReader);

        var kernelFactory = new KernelFactory(_serviceProvider);

        AIService = new MarsAIService(optionService, nodesReader, aiToolScenarioProvidersLocator, kernelFactory);

        AIToolService = Substitute.For<IAIToolService>();

        AIToolService.Prompt(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(args =>
            {
                var prompt = args.Arg<string>();
                return AIService.Reply(prompt);
            });

    }

    public Task<string> AiRequest(string prompt)
    {
        return AIService.Reply(prompt, SystemPrompt);
    }
}
