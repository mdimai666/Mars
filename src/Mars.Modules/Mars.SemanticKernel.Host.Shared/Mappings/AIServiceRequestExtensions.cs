using Mars.SemanticKernel.Host.Shared.Dto;
using Mars.SemanticKernel.Shared.Contracts;

namespace Mars.SemanticKernel.Host.Shared.Mappings;

public static class AIServiceRequestExtensions
{
    public static AITextRequest ToQuery(this AIServiceRequest request)
        => new()
        {
            Prompt = request.Prompt,
        };
}
