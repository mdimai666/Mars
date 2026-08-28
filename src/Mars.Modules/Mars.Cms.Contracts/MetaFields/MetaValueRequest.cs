namespace Mars.Cms.Contracts.MetaFields;

public record CreateMetaValueRequest : MetaValueDetailBase
{
    public required Guid MetaFieldId { get; init; }

}

public record UpdateMetaValueRequest : MetaValueDetailBase
{
    public required Guid MetaFieldId { get; init; }
}
