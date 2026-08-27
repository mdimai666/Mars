using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.MetaFields;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Services;

internal class MetaQueryFieldResolver : IMetaQueryFieldResolver
{
    private readonly MarsDbContext _marsDbContext;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IMetaFieldMaterializerService _metaFieldMaterializer;

    public MetaQueryFieldResolver(
        MarsDbContext marsDbContext,
        IMetaModelTypesLocator metaModelTypesLocator,
        IMetaFieldMaterializerService metaFieldMaterializer)
    {
        _marsDbContext = marsDbContext;
        _metaModelTypesLocator = metaModelTypesLocator;
        _metaFieldMaterializer = metaFieldMaterializer;
    }

    public async Task<Dictionary<string, Dictionary<Guid, object?>>> ResolveAsync(
        PostTypeDetail postType,
        IReadOnlyCollection<Guid> postIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, Dictionary<Guid, object?>>();

        var queryFields = postType.MetaFields.Where(f => f.Type == MetaFieldType.Query).ToList();
        if (queryFields.Count == 0 || postIds.Count == 0) return result;

        foreach (var field in queryFields)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var def = field.GetQueryDefinition();
            if (def is null) continue;

            // пока поддержан только пресет обратной связи на цели Post.<typeName>
            var parts = def.Target.Split('.', 2);
            if (parts[0] != "Post" || parts.Length < 2) continue;

            var targetType = _metaModelTypesLocator.GetPostTypeByName(parts[1]);
            var backField = targetType?.MetaFields?.FirstOrDefault(f => f.Key == def.BackReferenceKey
                                                                     && f.Type == MetaFieldType.Relation);
            if (backField is null) continue;

            // целевые посты, чьё Relation-поле ссылается на текущие посты
            var links = await _marsDbContext.PostMetaValues.AsNoTracking()
                .Where(mv => mv.MetaFieldId == backField.Id
                          && mv.ModelId != null
                          && postIds.Contains(mv.ModelId.Value))
                .Select(mv => new { FromPostId = mv.PostId, ToPostId = mv.ModelId!.Value })
                .ToListAsync(cancellationToken);

            if (links.Count == 0)
            {
                result[field.Key] = postIds.ToDictionary(id => id, _ => (object?)Array.Empty<object?>());
                continue;
            }

            var targetIds = links.Select(l => l.FromPostId).Distinct().ToList();
            var models = await _metaFieldMaterializer.GetModelByIds(
                new MetaFieldMaterializerQuery { Ids = targetIds, Type = MetaFieldType.Relation, ModelName = def.Target },
                cancellationToken);

            result[field.Key] = postIds.ToDictionary(
                id => id,
                id => (object?)links.Where(l => l.ToPostId == id)
                                    .Select(l => models.GetValueOrDefault(l.FromPostId)?.ModelDto)
                                    .ToArray());
        }

        return result;
    }
}
