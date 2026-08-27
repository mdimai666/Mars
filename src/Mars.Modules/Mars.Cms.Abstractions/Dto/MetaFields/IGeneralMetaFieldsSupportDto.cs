namespace Mars.Host.Shared.Dto.MetaFields;

public interface IGeneralMetaFieldsSupportDto
{
    IReadOnlyCollection<MetaFieldDto> MetaFields { get; }
}
