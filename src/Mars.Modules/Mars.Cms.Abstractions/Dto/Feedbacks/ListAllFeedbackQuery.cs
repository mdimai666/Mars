namespace Mars.Cms.Abstractions.Dto.Feedbacks;

public record ListAllFeedbackQuery
{
    public IReadOnlyCollection<Guid>? Ids { get; init; }
}
