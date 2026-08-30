namespace Mars.Identity.Abstractions.Dto.Users;

public record DeleteManyUserQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
