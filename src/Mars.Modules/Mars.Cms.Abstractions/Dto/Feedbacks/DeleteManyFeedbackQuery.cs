namespace Mars.Cms.Abstractions.Dto.Feedbacks;

public record DeleteManyFeedbackQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
