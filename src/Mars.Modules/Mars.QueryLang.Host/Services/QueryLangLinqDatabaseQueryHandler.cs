using System.Reflection;
using Mars.Core.Features;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.QueryLang.Services;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Mars.Host.Shared.WebSite.Models;
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

    public Task<object?> Handle(string linqExpression, PageRenderContext pageContext, Dictionary<string, object>? localVaribles, CancellationToken cancellationToken)
    {
        var efPropertyName = linqExpression.Split('.', 2)[0];

        var ppt = new XInterpreter(pageContext, localVaribles);
        var chains = TextHelper.ParseChainPairKeyValue(linqExpression);

        var xEntityType = FindEntityDbSetByPropertyName(efPropertyName);
        IQueryable? query;

        if (xEntityType is not null) query = GetEntitySbSet(xEntityType);
        else (xEntityType, query) = GetMetaTypeQuerySet(efPropertyName);

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
            result = instance.InvokeMethod(methodName, args);
        }

        return Task.FromResult(result);
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
                                                .AsNoTracking();
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

        var prop = dbContext.GetType().GetProperty(entityName);//?? throw new ArgumentException($"Не найдено свойство с именем '{entityName}' в контексте.");

        var entityType = prop?.PropertyType.GenericTypeArguments[0];

        return entityType;
    }
}
