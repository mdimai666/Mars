using Mars.SemanticKernel.Abstractions.Dto;
using Mars.SemanticKernel.Contracts.AIService;

namespace Mars.SemanticKernel.Abstractions.Mappings;

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
