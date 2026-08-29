namespace Mars.Options.Abstractions.Dto;

public record UpdateOptionQuery<T>
{
    public required string Key { get; init; }
    public required T Value { get; init; }
}
