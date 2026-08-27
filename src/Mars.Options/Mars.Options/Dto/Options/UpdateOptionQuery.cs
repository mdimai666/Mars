namespace Mars.Options.Dto.Options;

public record UpdateOptionQuery<T>
{
    public required string Key { get; init; }
    public required T Value { get; init; }
}
