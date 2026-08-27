namespace Mars.Host.Shared.Dto.Feedbacks;

public record DeleteManyFeedbackQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
