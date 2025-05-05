namespace Mars.Host.Shared.Dto.MetaFields;

public record MetaFieldVariantDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required float Value { get; init; }
    public required bool Disable { get; init; }
}
