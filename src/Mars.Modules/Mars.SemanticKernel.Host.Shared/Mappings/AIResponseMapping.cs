using Mars.SemanticKernel.Host.Shared.Dto;
using Mars.SemanticKernel.Shared.Contracts;

namespace Mars.SemanticKernel.Host.Shared.Mappings;

public static class AIResponseMapping
{
    public static AIServiceResponse ToResponse(this AIResponseDto entity)
        => new()
        {
            Content = entity.Content,
        };

    public static AIConfigNodeResponse ToResponse(this AIConfigNodeDto entity)
        => new()
        {
            NodeId = entity.NodeId,
            Title = entity.Title,
            Description = entity.Description,
        };

    public static IReadOnlyCollection<AIConfigNodeResponse> ToResponse(this IReadOnlyCollection<AIConfigNodeDto> list)
        => list.Select(ToResponse).ToArray();
}
