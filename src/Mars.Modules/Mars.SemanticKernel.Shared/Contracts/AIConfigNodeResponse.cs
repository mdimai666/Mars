namespace Mars.SemanticKernel.Shared.Contracts;

public record AIConfigNodeResponse
{
    public required string NodeId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
}
