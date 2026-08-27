namespace Mars.SemanticKernel.Abstractions.Dto;

public record AIConfigNodeDto
{
    public required string NodeId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
}
