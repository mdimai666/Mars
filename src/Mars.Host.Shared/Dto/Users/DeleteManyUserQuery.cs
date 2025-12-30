namespace Mars.Host.Shared.Dto.Users;

public record DeleteManyUserQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
