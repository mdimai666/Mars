namespace Mars.Shared.Contracts.AIService;

public record AIServiceRequest
{
    public required string Prompt { get; init; }
}

public record AIServiceToolRequest
{
    public required string Prompt { get; init; }
    public required string? ToolName { get; init; }
}
