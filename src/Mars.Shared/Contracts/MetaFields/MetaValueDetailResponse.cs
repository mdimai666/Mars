namespace Mars.Shared.Contracts.MetaFields;

public record MetaValueDetailResponse : MetaValueDetailBase
{
    public required MetaFieldDetailResponse? MetaField { get; init; }
}
