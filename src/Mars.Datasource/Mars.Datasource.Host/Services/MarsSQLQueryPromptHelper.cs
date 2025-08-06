using Mars.Host.Shared.Attributes;
using Mars.Host.Shared.Services;

namespace Mars.Datasource.Host.Services;

[RegisterAITool(Tags = ["sql"], Description = "Provides a database structure to help AI")]
public class MarsSQLQueryPromptHelper(IDatasourceAIToolSchemaProviderHandler handler, IAIToolService aiTool) : IAIToolScenarioProvider
{
    public async Task<string> Handle(string promptQuery, CancellationToken cancellationToken)
    {
        var structure = await handler.Handle();

        var promptText = $$"""
            Your task is to generate a valid SQL query (PostgreSQL) based solely on the schema below.

            SCHEMA:

            {{structure}}

            ---

            INSTRUCTION:

            {{promptQuery}}

            Return only the raw SQL.
            Do NOT use triple backticks (```).
            Do not explain or comment.
            Do not include any formatting like ```sql or explanations.
            """;

        var response = await aiTool.Prompt(promptText, cancellationToken);

        return response.Trim('`');
    }
}
