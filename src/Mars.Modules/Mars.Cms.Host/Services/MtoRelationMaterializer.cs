using System.Collections;
using System.Reflection;
using Mars.Data.Contexts;
using Mars.Data.Entities;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Host.Services;
using Mars.Contracts.MetaFields;
using Microsoft.EntityFrameworkCore;

namespace Mars.Cms.Host.Services;

internal class MtoRelationMaterializer : IMtoRelationMaterializer
{
    // имя статического поля селекта совпадает с GenSourceCodeMaster.selectExpressionGetterName
    const string SelectExpressionFieldName = "selectExpression";

    private readonly MarsDbContext _marsDbContext;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;

    public MtoRelationMaterializer(MarsDbContext marsDbContext, IMetaModelTypesLocator metaModelTypesLocator)
    {
        _marsDbContext = marsDbContext;
        _metaModelTypesLocator = metaModelTypesLocator;
    }

    public async Task FillAsync(string typeName, IEnumerable items, CancellationToken cancellationToken)
    {
        var list = items.Cast<object>().Where(s => s is not null).ToList();
        if (list.Count == 0) return;

        var postType = _metaModelTypesLocator.GetPostTypeByName(typeName);
        var relationFields = postType?.MetaFields?
            .Where(f => f.Type == MetaFieldType.Relation && !string.IsNullOrEmpty(f.ModelName))
            .ToList();
        if (relationFields is null || relationFields.Count == 0) return;

        var itemType = list[0].GetType();

        foreach (var field in relationFields)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var idProp = itemType.GetProperty($"{field.Key}Id");
            var navProp = itemType.GetProperty(field.Key);
            if (idProp is null || navProp is null || !navProp.CanWrite) continue;

            var ids = list.Select(s => idProp.GetValue(s) as Guid?)
                          .Where(g => g.HasValue)
                          .Select(g => g!.Value)
                          .Distinct()
                          .ToList();
            if (ids.Count == 0) continue;

            var targets = await LoadTargetsAsync(field.ModelName!, ids, cancellationToken);
            if (targets.Count == 0) continue;

            foreach (var item in list)
            {
                if (idProp.GetValue(item) is Guid id
                    && targets.TryGetValue(id, out var target)
                    && navProp.PropertyType.IsInstanceOfType(target))
                {
                    navProp.SetValue(item, target);
                }
            }
        }
    }

    async Task<Dictionary<Guid, object>> LoadTargetsAsync(string modelName, List<Guid> ids, CancellationToken cancellationToken)
    {
        var parts = modelName.Split('.', 2);
        var root = parts[0];
        string? subType = parts.Length > 1 ? parts[1] : null;

        switch (root)
        {
            case "Post" when subType is null:
                return (await _marsDbContext.Posts.AsNoTracking()
                                            .Where(s => ids.Contains(s.Id))
                                            .ToListAsync(cancellationToken))
                        .ToDictionary(s => s.Id, s => (object)s);

            case "Post":
                return await LoadMtoTargetsAsync(subType!, ids, cancellationToken);

            case "User":
                return (await _marsDbContext.Users.AsNoTracking()
                                            .Where(s => ids.Contains(s.Id))
                                            .ToListAsync(cancellationToken))
                        .ToDictionary(s => s.Id, s => (object)s);

            case "File":
                return (await _marsDbContext.Files.AsNoTracking()
                                            .Where(s => ids.Contains(s.Id))
                                            .ToListAsync(cancellationToken))
                        .ToDictionary(s => s.Id, s => (object)s);

            case "Feedback":
                return (await _marsDbContext.Feedbacks.AsNoTracking()
                                            .Where(s => ids.Contains(s.Id))
                                            .ToListAsync(cancellationToken))
                        .ToDictionary(s => s.Id, s => (object)s);

            case "NavMenu":
                return (await _marsDbContext.NavMenus.AsNoTracking()
                                            .Where(s => ids.Contains(s.Id))
                                            .ToListAsync(cancellationToken))
                        .ToDictionary(s => s.Id, s => (object)s);

            default:
                return [];
        }
    }

    /// <summary>
    /// Цели вида Post.&lt;typeName&gt;: тем же скомпилированным селектом целевого типа
    /// </summary>
    async Task<Dictionary<Guid, object>> LoadMtoTargetsAsync(string typeName, List<Guid> ids, CancellationToken cancellationToken)
    {
        _metaModelTypesLocator.TryUpdateMetaModelMtoRuntimeCompiledTypes();
        if (!_metaModelTypesLocator.MetaMtoModelsCompiledTypeDict.TryGetValue(typeName, out var mto)) return [];

        var selectExpression = mto.CreatedType
            .GetField(SelectExpressionFieldName, BindingFlags.Static | BindingFlags.Public)?
            .GetValue(null);
        if (selectExpression is null) return [];

        var query = _marsDbContext.Posts.AsNoTracking()
                                    .Where(s => s.PostType.TypeName == typeName && ids.Contains(s.Id));

        var selectMethod = typeof(Queryable)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .First(mi => mi.Name == nameof(Queryable.Select)
                        && mi.IsGenericMethodDefinition
                        && mi.GetParameters().Length == 2
                        && mi.GetParameters()[1].Name == "selector")
            .MakeGenericMethod(typeof(PostEntity), mto.CreatedType);

        var projected = (IEnumerable)selectMethod.Invoke(null, [query, selectExpression])!;

        var idProp = mto.CreatedType.GetProperty(nameof(Mars.Core.Interfaces.IHasId.Id))!;
        var result = new Dictionary<Guid, object>();
        foreach (var item in projected)
        {
            result[(Guid)idProp.GetValue(item)!] = item;
        }
        return result;
    }
}
