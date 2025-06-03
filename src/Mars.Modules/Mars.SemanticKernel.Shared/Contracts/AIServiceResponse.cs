namespace Mars.SemanticKernel.Shared.Contracts;

public record AIServiceResponse
{
    public required string Content { get; init; }
}
