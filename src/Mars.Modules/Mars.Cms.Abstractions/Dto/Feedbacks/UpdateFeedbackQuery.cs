namespace Mars.Cms.Abstractions.Dto.Feedbacks;

public record UpdateFeedbackQuery
{
    public required Guid Id { get; init; }
    public required string Type { get; init; }
}
