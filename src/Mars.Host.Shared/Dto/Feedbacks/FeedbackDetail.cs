using Mars.Core.Interfaces;

namespace Mars.Host.Shared.Dto.Feedbacks;

public record FeedbackDetail : IHasId
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Title { get; init; }
    public required string? Phone { get; init; }
    public required string? Email { get; init; }
    public required string Type { get; init; }
    public required bool IsAuthorizedUser { get; init; }
    public required string FilledUsername { get; init; }
    public required string Content { get; init; }

}
