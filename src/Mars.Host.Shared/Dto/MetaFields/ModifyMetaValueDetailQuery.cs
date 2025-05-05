namespace Mars.Host.Shared.Dto.MetaFields;

public record ModifyMetaValueDetailQuery : ModifyMetaValueDetailDto
{
    public required MetaFieldDto MetaField { get; init; }
}
