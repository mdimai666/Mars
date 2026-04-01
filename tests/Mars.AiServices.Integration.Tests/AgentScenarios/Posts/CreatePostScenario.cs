using FluentAssertions;
using Mars.AiServices.Integration.Tests.Common;

namespace Mars.AiServices.Integration.Tests.AgentScenarios.Posts;

public class CreatePostScenario : ScenarioTestBase
{
    public override string SystemPrompt { get; } = """
        Ты агент, который создает посты на сайте.
        Отвечай только на вопрос, не пиши объяснений.
        """;

    [Fact]
    public async Task CreatePost_CreateByPrompt_ShouldSuccess()
    {
        var response = await AiRequest("сколько будет 2+2");

        response.Should().Be("4");
    }
}
