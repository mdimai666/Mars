using FluentAssertions;
using Mars.AiServices.Integration.Tests.Common;
using Mars.SemanticKernel.CMS.Posts;
using NSubstitute;

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
        _ = nameof(AiCreatePostHandler);

        var userPrompt = """
                    ## Контекст:
                    Придумай интересный, вовлекающий пост про язык программирования C#. 
                    Пост должен быть полезным для разработчиков разного уровня — от начинающих до опытных.

                    ## Требования к посту:
                    1. Заголовок должен привлекать внимание и содержать ключевое слово "C#"
                    2. Основной контент должен быть структурирован (используй маркированные списки или подзаголовки)
                    3. Добавь практические примеры кода (будут в секции codeExamples)
                    4. В конце призови к действию (лайк, комментарий, подписка)
                    5. Используй современный C# (версии 10-12) и новейшие возможности языка
                    6. Тон поста: дружелюбный, экспертный, но без излишнего снобизма

                    ## Жанр поста:
                    Технический блог-пост для соцсетей (LinkedIn, Telegram, Twitter/X) или Dev.to

                    ## Дополнительные указания:
                    - Длина: 500-800 слов
                    - Добавь 3-5 хештегов в конце
                    - Используй реальные примеры из практики
                    """;

        var tool = new AiCreatePostTool(AIToolService, Substitute.For<IAiCreatePostHandler>());

        var action = () => tool.Handle(userPrompt, default);

        await action.Should().NotThrowAsync();
    }
}
