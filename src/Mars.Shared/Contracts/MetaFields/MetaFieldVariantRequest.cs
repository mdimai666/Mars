namespace Mars.Shared.Contracts.MetaFields;

public record MetaFieldVariantBase
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required float Value { get; init; }
    public required bool Disable { get; init; }
}

public record CreateMetaFieldVariantRequest : MetaFieldVariantBase
{

}

public record UpdateMetaFieldVariantRequest : MetaFieldVariantBase
{

}
