namespace Mars.Host.Shared.Dto.Options;

public record CreateOptionQuery<T>
{
    public Guid? Id { get; init; }
    public required string Key { get; init; }
    public required T Value { get; init; }

}
