namespace Mars.Cms.Contracts.MetaFields;

public record MetaFieldDetailResponse : MetaFieldDetailBase
{
    public required IReadOnlyCollection<MetaFieldVariantResponse>? Variants { get; init; }
}
