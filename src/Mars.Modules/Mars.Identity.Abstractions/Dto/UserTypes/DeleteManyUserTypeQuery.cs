namespace Mars.Host.Shared.Dto.UserTypes;

public record DeleteManyUserTypeQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

}
