using System.Text.Json;
using Mars.Core.Features.JsonConverter;
using Mars.Host.Shared.Attributes;
using Mars.Host.Shared.Services;
using Mars.SemanticKernel.Host.Shared.Generators;

namespace Mars.SemanticKernel.CMS.Posts;

[RegisterAITool(Tags = ["post"], Description = "Создает пост на основе промпта")]
internal class AiCreatePostTool(IAIToolService aiTool, IAiCreatePostHandler handler) : IAIToolScenarioProvider
{

    public async Task<string> Handle(string userPrompt, CancellationToken cancellationToken)
    {
        var generator = new ModelJsonSchemaGenerator();
        var postSchema = generator.GenerateSchema<AiCreatePostQuery>();

        var promptText = $"""
                    Ты — профессиональный копирайтер.

                    Твоя задача — создать пост на основе следующей JSON-схемы, которая описывает ожидаемую структуру ответа.

                    ## Ожидаемая схема ответа:
                    ```json
                    {postSchema}
                    ```
                    ---
                    Запрос пользователя:
                    {userPrompt}
                    ---
                    Сгенерируй пост, строго следуя JSON-схеме. 
                    """;

        var response = await aiTool.Prompt(promptText, cancellationToken);

        //var postJson = LlmResponseTrimmer.TrimResponse(response);
        var postJson = QuickMdQuoteTrim.Clean(response);
        //var postJson = LlmResponseTrimmer.DeserializeFromResponse response.Trim('`');

        var createPostQuery = JsonSerializer.Deserialize<AiCreatePostQuery>(postJson, SystemJsonConverter.DefaultJsonSerializerOptions())!;
        await handler.Handle(createPostQuery, cancellationToken);

        return $"Post created: '{createPostQuery.Title}'";
    }
}
