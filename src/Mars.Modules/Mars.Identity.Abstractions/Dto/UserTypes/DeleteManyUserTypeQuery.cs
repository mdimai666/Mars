namespace Mars.Identity.Abstractions.Dto.UserTypes;

public record DeleteManyUserTypeQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
