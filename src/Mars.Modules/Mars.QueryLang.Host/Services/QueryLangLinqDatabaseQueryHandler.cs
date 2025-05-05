using System.Reflection;
using Mars.Core.Features;
using Mars.Host.Data.Contexts;
using Mars.Host.Shared.QueryLang.Services;
using Mars.Host.Shared.Templators;
using Mars.Host.Shared.WebSite.Models;
using Microsoft.EntityFrameworkCore;

namespace Mars.QueryLang.Host.Services;

/*
 TODO:
[x] - query by Meta fialds as .Where(post.ineMetaFiled>2);
 
 */

public class QueryLangLinqDatabaseQueryHandler(
    MarsDbContext MarsDbContext
    ) : IQueryLangLinqDatabaseQueryHandler
{
    public Task<object?> Handle(string linqExpression, PageRenderContext pageContext, Dictionary<string, object>? localVaribles, CancellationToken cancellationToken)
    {
        var efPropertyName = linqExpression.Split('.', 2)[0];

        var ppt = new XInterpreter(pageContext, localVaribles);
        var chains = TextHelper.ParseChainPairKeyValue(linqExpression);

        var xEntityType = FindEntityDbSetByPropertyName(efPropertyName);
        var query = GetEntitySbSet(xEntityType);

        var xefType = typeof(EfStringQuery<>);
        Type[] typeArgs = { xEntityType };
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
        var dbContext = MarsDbContext;
        MethodInfo method = dbContext.GetType().GetMethod(nameof(DbContext.Set), BindingFlags.Public | BindingFlags.Instance, [])!;
        method = method.MakeGenericMethod(entityType);
        return (method.Invoke(dbContext, null) as IQueryable)!;
    }

    Type FindEntityDbSetByPropertyName(string entityName)
    {
        var dbContext = MarsDbContext;

        var prop = dbContext.GetType().GetProperty(entityName) ?? throw new ArgumentException($"Не найдено свойство с именем '{entityName}' в контексте.");
        var entityType = prop.PropertyType.GenericTypeArguments[0];

        return entityType;
    }
}
