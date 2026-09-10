namespace Mars.Cms.Contracts.MetaFields;

public record MetaValueDetailResponse : MetaValueDetailBase
{
    public required MetaFieldDetailResponse? MetaField { get; init; }
}
