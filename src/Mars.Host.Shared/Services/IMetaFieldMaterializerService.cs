using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Services;

public interface IMetaFieldMaterializerService
{
    Task<Dictionary<Guid, MetaFieldRelatedFillDictValue>> GetModelByIds(MetaFieldMaterializerQuery query, CancellationToken cancellationToken);
    Task<MetaFieldRelatedFillDict> GetFillContext(IEnumerable<MetaValueDto> metaValues, CancellationToken cancellationToken);
}

public record MetaFieldMaterializerQuery
{
    public required MetaFieldType Type { get; init; }

    /// <summary>
    /// model name combination
    /// <list type="bullet">
    /// <item>User</item>
    /// <item>Post.page</item>
    /// <item>Post.post</item>
    /// </list>
    /// </summary>
    public required string ModelName { get; init; }
    public required IReadOnlyCollection<Guid> Ids { get; init; }
}
