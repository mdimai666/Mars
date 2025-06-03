namespace Mars.SemanticKernel.Host.Shared.Dto;

public record AITextRequest
{
    public required string Prompt { get; init; }
}
