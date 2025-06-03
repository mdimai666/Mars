namespace Mars.SemanticKernel.Shared.Contracts;

public record AIServiceRequest
{
    public required string Prompt { get; init; }
}
