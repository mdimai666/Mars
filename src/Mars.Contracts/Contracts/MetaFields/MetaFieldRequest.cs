namespace Mars.Shared.Contracts.MetaFields;

public record class CreateMetaFieldRequest : MetaFieldDetailBase
{
    public required IReadOnlyCollection<CreateMetaFieldVariantRequest>? Variants { get; init; }

}

public record class UpdateMetaFieldRequest : MetaFieldDetailBase
{
    public required IReadOnlyCollection<UpdateMetaFieldVariantRequest>? Variants { get; init; }
}
