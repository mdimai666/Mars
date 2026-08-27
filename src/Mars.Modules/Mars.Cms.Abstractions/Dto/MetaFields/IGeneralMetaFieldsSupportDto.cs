namespace Mars.Cms.Abstractions.Dto.MetaFields;

public interface IGeneralMetaFieldsSupportDto
{
    IReadOnlyCollection<MetaFieldDto> MetaFields { get; }
}
