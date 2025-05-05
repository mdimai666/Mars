namespace Mars.Host.Shared.Dto.Feedbacks;

public record FeedbackSummary
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Title { get; init; }
    public required string? Phone { get; init; }
    public required string? Email { get; init; }
    public required string Type { get; init; }
    public required bool IsAuthorizedUser { get; init; }
    public required string Excerpt { get; init; }
    public required string FilledUsername { get; init; }
}
