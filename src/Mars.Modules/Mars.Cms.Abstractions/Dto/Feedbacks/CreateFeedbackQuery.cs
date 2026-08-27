namespace Mars.Host.Shared.Dto.Feedbacks;

public record CreateFeedbackQuery
{
    public required string Title { get; init; }
    public required string? Phone { get; init; }
    public required string? Email { get; init; }
    public required string Type { get; init; }
    public required string FilledUsername { get; init; }
    public required string Content { get; init; }
    public required Guid? AuthorizedUserId { get; init; }

}

public enum FeedbackType : int
{
    InfoMessage,
    BugReport,
    Question
}
