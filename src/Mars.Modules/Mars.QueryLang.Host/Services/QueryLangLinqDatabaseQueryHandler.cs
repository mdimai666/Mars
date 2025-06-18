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

    public QueryLangLinqDatabaseQueryHandler(MarsDbContext MarsDbContext, IMetaModelTypesLocator? metaModelTypesLocator)
    {
        _marsDbContext = MarsDbContext;
        _metaModelTypesLocator = metaModelTypesLocator;
    }

    public async Task<object?> Handle(string linqExpression, XInterpreter ppt, CancellationToken cancellationToken)
    {
        if (linqExpression.StartsWith("Union(")) return await ExecuteUnionExpressions(linqExpression, ppt, cancellationToken);

        var efPropertyName = linqExpression.Trim().Split('.', 2)[0];

        //var ppt = new XInterpreter(pageContext, localVaribles);
        var chains = TextHelper.ParseChainPairKeyValue(linqExpression);

        var xEntityType = FindEntityDbSetByPropertyName(efPropertyName);
        IQueryable? query;

        xEntityType ??= FindEntityDbSetByTypeName(efPropertyName);

        xEntityType ??= FindEntityDbSetByTypeName(efPropertyName + "Entity");

        var isMetaType = false;

        if (xEntityType is not null) query = GetEntitySbSet(xEntityType);
        else
        {
            (xEntityType, query) = GetMetaTypeQuerySet(efPropertyName);
            isMetaType = true;
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

                var efExpression = await new QueryLangLinqDatabaseQueryHandler(_marsDbContext, _metaModelTypesLocator).Handle(args, ppt, cancellationToken);
                var internalQuery = (efExpression as IDynamicQueryableObject).GetQuery();

                result = instance.InvokeMethodArgs(methodName, [internalQuery]);

                //return result.ToList;
            }
            else
            {
                result = instance.InvokeMethod(methodName, args);
            }
        }

        return result;
    }

    IQueryable GetEntitySbSet(Type entityType)
    {
        var dbContext = _marsDbContext;
        MethodInfo method = dbContext.GetType().GetMethod(nameof(DbContext.Set), BindingFlags.Public | BindingFlags.Instance, [])!;
        method = method.MakeGenericMethod(entityType);
        return (method.Invoke(dbContext, null) as IQueryable)!;
    }

    (Type? entityType, IQueryable?) GetMetaTypeQuerySet(string typeName)
    {
        _metaModelTypesLocator.TryUpdateMetaModelMtoRuntimeCompiledTypes();

        if (_metaModelTypesLocator.MetaMtoModelsCompiledTypeDict.TryGetValue(typeName, out var metaModelType))
        {
            var dbContext = _marsDbContext;

            if (typeof(PostEntity).IsAssignableFrom(metaModelType))
            {
                var query = _marsDbContext.Posts.Include(s => s.MetaValues!)
                                                    .ThenInclude(s => s.MetaField)
                                                .Include(s => s.User)
                                                .Include(s => s.PostType)
                                                .AsNoTracking()
                                                .Where(s => s.PostType.TypeName == typeName);
                return (metaModelType, ApplySelectExpression(query, metaModelType));
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        return (null, null); //throw new KeyNotFoundException($"IMetaModelTypesLocator metaType '{typeName}' not found");
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

    Type? FindEntityDbSetByPropertyName(string entityName)
    {
        var dbContext = _marsDbContext;

        _memberDbSetsByName ??= typeof(MarsDbContext).GetProperties()
                .Where(p => p.PropertyType.IsGenericType
                            && (p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)))
                .ToDictionary(s => s.Name);

        return _memberDbSetsByName.GetValueOrDefault(entityName)?.PropertyType;
    }

    static Dictionary<string, PropertyInfo>? _memberDbSetsByName;
    static Dictionary<string, PropertyInfo>? _memberDbSetsByTypeName;

    Type? FindEntityDbSetByTypeName(string typeName)
    {
        var dbContext = _marsDbContext;

        _memberDbSetsByTypeName ??= typeof(MarsDbContext).GetProperties()
                .Where(p => p.PropertyType.IsGenericType
                            && (p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)))
                .ToDictionary(s => s.PropertyType.GenericTypeArguments[0].Name);

        return _memberDbSetsByTypeName.GetValueOrDefault(typeName)?.PropertyType.GenericTypeArguments[0];
    }

    async Task<IEnumerable<object>> ExecuteUnionExpressions(string linqExpression, XInterpreter ppt, CancellationToken cancellationToken)
    {
        var chains = TextHelper.ParseArguments(linqExpression);

        var items = new Dictionary<TypedIdKey, object>();

        foreach (var args in chains)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var efExpression = await new QueryLangLinqDatabaseQueryHandler(_marsDbContext, _metaModelTypesLocator).Handle(args, ppt, cancellationToken);
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
