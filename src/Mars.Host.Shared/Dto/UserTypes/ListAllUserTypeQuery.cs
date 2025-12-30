namespace Mars.Host.Shared.Dto.UserTypes;

public record ListAllUserTypeQuery
{
    public IReadOnlyCollection<Guid>? Ids { get; init; }
}
