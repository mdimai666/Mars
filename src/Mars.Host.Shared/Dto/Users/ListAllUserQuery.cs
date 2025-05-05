namespace Mars.Host.Shared.Dto.Users;

public record ListAllUserQuery
{
    public IReadOnlyCollection<Guid>? Ids { get; init; }
}
