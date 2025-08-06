namespace Mars.Shared.Contracts.AIService;

public record AIServiceResponse
{
    public required string Content { get; init; }
}
