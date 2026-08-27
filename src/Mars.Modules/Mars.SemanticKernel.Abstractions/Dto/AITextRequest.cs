namespace Mars.SemanticKernel.Host.Shared.Dto;

public record AITextRequest
{
    public required string Prompt { get; init; }
}

public record AITextToolRequest
{
    public required string Prompt { get; init; }
    public required string? ToolName { get; init; }
}
