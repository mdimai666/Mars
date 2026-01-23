using System.Reflection;
using Mars.Core.Features;
using Mars.Host.Data.Common;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.QueryLang.Services;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Mars.MetaModelGenerator;
using Microsoft.EntityFrameworkCore;

namespace Mars.QueryLang.Host.Services;

public class QueryLangLinqDatabaseQueryHandler : IQueryLangLinqDatabaseQueryHandler
{
    private readonly MarsDbContext _marsDbContext;
    private readonly IMetaModelTypesLocator? _metaModelTypesLocator;
    private readonly IDatabaseEntityTypeCatalogService _databaseEntityTypeCatalogService;

    public QueryLangLinqDatabaseQueryHandler(MarsDbContext MarsDbContext,
                                            IMetaModelTypesLocator? metaModelTypesLocator,
                                            IDatabaseEntityTypeCatalogService databaseEntityTypeCatalogService)
    {
        _marsDbContext = MarsDbContext;
        _metaModelTypesLocator = metaModelTypesLocator;
        _databaseEntityTypeCatalogService = databaseEntityTypeCatalogService;
    }

    public Task<object?> Handle(string linqExpression, XInterpreter ppt, CancellationToken cancellationToken)
    {
        return Handle(linqExpression, ppt, true, cancellationToken);
    }

    private async Task<object?> Handle(string linqExpression, XInterpreter ppt, bool autoCompleteWithList, CancellationToken cancellationToken)
    {
        if (linqExpression.StartsWith("Union(")) return await ExecuteUnionExpressions(linqExpression, ppt, cancellationToken);

        var efPropertyName = linqExpression.Trim().Split('.', 2)[0];

        //var ppt = new XInterpreter(pageContext, localVaribles);
        var chains = TextHelper.ParseChainPairKeyValue(linqExpression);

        //var resolveResult = _databaseEntityTypeCatalogService.ResolveName(efPropertyName);
        var resolveResult = _metaModelTypesLocator.ResolveEntityNameToSourceUri(efPropertyName);
        if (resolveResult is null)
            throw new InvalidOperationException($"ef entity '{efPropertyName}' or MetaType not found");

        IQueryable? query;
        Type xEntityType = resolveResult.MetaEntityModelType;
        var isMetaType = resolveResult.IsMetaType;

        if (!isMetaType)
        {
            query = GetEntityDbSet(xEntityType);
        }
        else
        {
            query = GetMetaTypeQuerySet(resolveResult);
        }

        if (query is null)
        {
            throw new InvalidOperationException($"ef direct property '{efPropertyName}' of MetaType not found");
        }

        var xefType = typeof(EfStringQuery<>);
        Type[] typeArgs = { xEntityType! };
        var xefGenericType = xefType.MakeGenericType(typeArgs);
        var instance = Activator.CreateInstance(xefGenericType, [query, ppt])! as IDynamicQueryableObject;

        object? result = null;
        //var invokeMethod = xefGenericType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
        //                                    .Single(mi => mi.Name == nameof(EfStringQuery<object>.InvokeMethod));

        foreach (var (methodName, args) in chains)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (methodName == nameof(Queryable.Union))
            {
                if (isMetaType)
                {
                    throw new NotImplementedException("Union for metaTypes not work yet. Please use 'ef.Union(arr1,arr2) method. \nLike: 'posts = ef.Union(myType.Take(1),posts.Where(post.Slug=123))'");
                }

                var efExpression = await new QueryLangLinqDatabaseQueryHandler(_marsDbContext, _metaModelTypesLocator, _databaseEntityTypeCatalogService)
                                                    .Handle(args, ppt, autoCompleteWithList: false, cancellationToken);
                var internalQuery = (efExpression as IDynamicQueryableObject).GetQuery();

                result = instance.InvokeMethodArgs(methodName, [internalQuery]);

                //return result.ToList;
            }
            else
            {
                result = instance.InvokeMethod(methodName, args);
            }
        }

        if (autoCompleteWithList && result is IQueryable)
        {
            result = instance.InvokeMethod(nameof(EfStringQuery<>.ToList), "");
        }

        return result;
    }

    IQueryable GetEntityDbSet(Type entityType)
    {
        // DbContext.Set<TEntity>()
        var dbContext = _marsDbContext;
        MethodInfo methodSet = dbContext.GetType().GetMethod(nameof(DbContext.Set), BindingFlags.Public | BindingFlags.Instance, [])!;
        methodSet = methodSet.MakeGenericMethod(entityType);
        var query = (methodSet.Invoke(dbContext, null) as IQueryable)!;

        // QueryableExtensions.AsNoTracking<TEntity>(IQueryable<TEntity>)
        MethodInfo asNoTrackingMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m =>
                m.Name == nameof(EntityFrameworkQueryableExtensions.AsNoTracking) &&
                m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 1);

        asNoTrackingMethod = asNoTrackingMethod.MakeGenericMethod(entityType);

        query = (IQueryable)asNoTrackingMethod.Invoke(null, [query])!;

        return query;
    }

    IQueryable? GetMetaTypeQuerySet(MetaModelSourceResult metaModelType)
    {
        _metaModelTypesLocator.TryUpdateMetaModelMtoRuntimeCompiledTypes();

        var dbContext = _marsDbContext;

        if (typeof(PostEntity) == metaModelType.BaseEntityType)
        {
            var query = _marsDbContext.Posts.Include(s => s.MetaValues!)
                                                .ThenInclude(s => s.MetaField)
                                            .Include(s => s.User)
                                            .Include(s => s.PostType)
                                            .AsNoTracking()
                                            .Where(s => s.PostType.TypeName == metaModelType.EntityUri[1]);
            return ApplySelectExpression(query, metaModelType.MetaEntityModelType);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    IQueryable? ApplySelectExpression(IQueryable<PostEntity> query, Type compiledType)
    {
        var selectExpression = compiledType.GetField(GenSourceCodeMaster.selectExpressionGetterName, BindingFlags.Static | BindingFlags.Public).GetValue(null)!;

        MethodInfo selectMethod = typeof(Queryable)
              .GetMethods(BindingFlags.Static | BindingFlags.Public)
              .First(mi => mi.Name == nameof(Queryable.Select)
                         && mi.IsGenericMethodDefinition
                         && mi.GetParameters().Length == 2
                && mi.GetParameters()[1].Name == "selector")
              .MakeGenericMethod(typeof(PostEntity), compiledType);

        return selectMethod.Invoke(query, [query, selectExpression]) as IQueryable;
    }

    async Task<IEnumerable<object>> ExecuteUnionExpressions(string linqExpression, XInterpreter ppt, CancellationToken cancellationToken)
    {
        var chains = TextHelper.ParseArguments(linqExpression);

        var items = new Dictionary<TypedIdKey, object>();

        foreach (var args in chains)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var efExpression = await new QueryLangLinqDatabaseQueryHandler(_marsDbContext, _metaModelTypesLocator, _databaseEntityTypeCatalogService)
                                            .Handle(args, ppt, autoCompleteWithList: false, cancellationToken);
            var internalQuery = (efExpression as IDynamicQueryableObject).GetQuery();

            //list.Add(internalQuery);
            foreach (var item in internalQuery)
            {
                var key = new TypedIdKey(item.GetType(), item is IBasicEntity be ? be.Id : throw new NotImplementedException($"Not found Id for type '{item.GetType()}'"));
                if (!items.ContainsKey(key))
                    items.Add(key, item);
            }
        }

        return items.Values;
    }
}

internal record TypedIdKey(Type Type, Guid Id);
