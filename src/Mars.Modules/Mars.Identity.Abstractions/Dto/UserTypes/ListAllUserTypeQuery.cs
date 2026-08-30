namespace Mars.Identity.Abstractions.Dto.UserTypes;

public record ListAllUserTypeQuery
{
    public IReadOnlyCollection<Guid>? Ids { get; init; }
}
