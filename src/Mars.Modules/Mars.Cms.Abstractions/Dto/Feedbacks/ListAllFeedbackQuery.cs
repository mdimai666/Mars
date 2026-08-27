namespace Mars.Host.Shared.Dto.Feedbacks;

public record ListAllFeedbackQuery
{
    public IReadOnlyCollection<Guid>? Ids { get; init; }
}
