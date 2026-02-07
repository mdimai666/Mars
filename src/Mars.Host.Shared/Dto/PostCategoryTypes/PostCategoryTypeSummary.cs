using Mars.Core.Interfaces;

namespace Mars.Host.Shared.Dto.PostCategoryTypes;

public record PostCategoryTypeSummary : IHasId
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Title { get; init; }
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
}
