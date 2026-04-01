using System.Text.Json;
using AutoFixture;
using Mars.Host.Shared.Services;
using Mars.SemanticKernel.Host.Service;
using Mars.SemanticKernel.Host.Shared.Interfaces;
using Mars.SemanticKernel.Shared.Nodes;
using Mars.SemanticKernel.Shared.Options;
using NSubstitute;

namespace Mars.AiServices.Integration.Tests.Common;

public abstract class ScenarioTestBase
{
    public readonly IFixture _fixture = new Fixture();
    public readonly IMarsAIService AIService;
    public abstract string SystemPrompt { get; }

    public ScenarioTestBase()
    {
        var modelConfig = new OllamaOptions()
        {
            ModelId = "gpt-oss:20b"
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
        AIService = new MarsAIService(optionService, nodesReader, aiToolScenarioProvidersLocator);
    }

    public Task<string> AiRequest(string prompt)
    {
        return AIService.Reply(prompt, SystemPrompt);
    }
}
