using Mars.Host.Shared.Attributes;
using Mars.Host.Shared.Services;

namespace Mars.SemanticKernel.CMS.Agents;

[RegisterAITool(Key = "cms_agent", Tags = ["post"], Description = "Сценарий для использования cms агента.")]
internal class CmsPostAgentHandler(CmsAgentHandler agent) : IAIToolScenarioProvider
{
    public async Task<string> Handle(string promptQuery, CancellationToken cancellationToken)
    {
        var result = await agent.Handle(promptQuery, cancellationToken: cancellationToken);

        return result.Content;
    }
}
