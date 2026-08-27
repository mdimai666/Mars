using Mars.SemanticKernel.Host.Shared.Dto;
using Mars.Shared.Contracts.AIService;

namespace Mars.SemanticKernel.Host.Shared.Mappings;

public static class AIServiceRequestExtensions
{
    public static AITextRequest ToQuery(this AIServiceRequest request)
        => new()
        {
            Prompt = request.Prompt,
        };

    public static AITextToolRequest ToQuery(this AIServiceToolRequest request)
        => new()
        {
            Prompt = request.Prompt,
            ToolName = request.ToolName,
        };
}
