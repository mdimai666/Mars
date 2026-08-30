namespace Mars.Identity.Abstractions.Dto.Users;

public record ListAllUserQuery
{
    public IReadOnlyCollection<Guid>? Ids { get; init; }

    public IReadOnlyCollection<string>? InRoles { get; init; }
}
