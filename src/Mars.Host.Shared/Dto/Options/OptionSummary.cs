namespace Mars.Host.Shared.Dto.Options;

public record OptionSummary
{
    public required string Key { get; init; }
    public required string Type { get; init; }
    public required string Value { get; init; }
}

public record OptionDetail : OptionSummary
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? ModifiedAt { get; init; }
}
