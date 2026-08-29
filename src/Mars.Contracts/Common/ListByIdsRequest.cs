namespace Mars.Contracts.Common;

public record ListByIdsRequest
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }
}
