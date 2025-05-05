namespace Mars.Shared.Contracts.MetaFields;

public record MetaFieldVariantValueResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required float Value { get; init; }
}
