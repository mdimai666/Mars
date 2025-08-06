using System.Linq.Expressions;
using System.Reflection;

namespace Mars.Core.Extensions;

public static class LinqExtensions //https://stackoverflow.com/a/32061921/6723966
{

    //ORDER BY
    private static PropertyInfo GetPropertyInfo(Type objType, string name)
    {
        var properties = objType.GetProperties();
        var matchedProperty = properties.FirstOrDefault(p => p.Name == name);
        if (matchedProperty == null)
            //throw new ArgumentException("name");
            return properties.First();

        return matchedProperty;
    }
    private static LambdaExpression GetOrderExpression(Type objType, PropertyInfo pi)
    {
        var paramExpr = Expression.Parameter(objType);
        var propAccess = Expression.PropertyOrField(paramExpr, pi.Name);
        var expr = Expression.Lambda(propAccess, paramExpr);
        return expr;
    }

    public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> query, string name)
    {
        var propInfo = GetPropertyInfo(typeof(T), name);
        var expr = GetOrderExpression(typeof(T), propInfo);
        if (expr is null) throw new ArgumentException($"order expression by \"{name}\" is null");

        MethodInfo? method = typeof(Enumerable).GetMethods().FirstOrDefault(m => m.Name == "OrderBy" && m.GetParameters().Length == 2);
        if (method == null) return query;
        MethodInfo genericMethod = method.MakeGenericMethod(typeof(T), propInfo.PropertyType);
        return (IEnumerable<T>)genericMethod.Invoke(null, new object[] { query, expr.Compile() })!;
    }

    public static IQueryable<T> OrderBy<T>(this IQueryable<T> query, string name)
    {
        var propInfo = GetPropertyInfo(typeof(T), name);
        var expr = GetOrderExpression(typeof(T), propInfo);
        if (expr is null) throw new ArgumentException($"order expression by \"{name}\" is null");

        var method = typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name == "OrderBy" && m.GetParameters().Length == 2);
        if (method == null) return query;

        var genericMethod = method.MakeGenericMethod(typeof(T), propInfo.PropertyType);
        return (IQueryable<T>)genericMethod.Invoke(null, new object[] { query, expr })!;
    }

    public static IEnumerable<T> OrderByDescending<T>(this IEnumerable<T> query, string name)
    {
        var propInfo = GetPropertyInfo(typeof(T), name);
        var expr = GetOrderExpression(typeof(T), propInfo);
        if (expr is null) throw new ArgumentException($"order expression by \"{name}\" is null");

        var method = typeof(Enumerable).GetMethods().FirstOrDefault(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2);
        if (method == null) return query;
        var genericMethod = method.MakeGenericMethod(typeof(T), propInfo.PropertyType);
        return (IEnumerable<T>)genericMethod.Invoke(null, new object[] { query, expr.Compile() })!;
    }

    public static IQueryable<T> OrderByDescending<T>(this IQueryable<T> query, string name)
    {
        var propInfo = GetPropertyInfo(typeof(T), name);
        var expr = GetOrderExpression(typeof(T), propInfo);

        var method = typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2);
        if (method == null) return query;
        var genericMethod = method.MakeGenericMethod(typeof(T), propInfo.PropertyType);
        return (IQueryable<T>)genericMethod.Invoke(null, new object[] { query, expr })!;
    }

    static (string field, bool asc) ParseMinusNameQuery(string orderQuery) //LocNameE ASC
    {
        if (string.IsNullOrEmpty(orderQuery) || orderQuery == "undefined" || orderQuery?.Length < 2)
        {
            return ("", true);
        }
        else
        {
            if (orderQuery is null) throw new ArgumentNullException(nameof(orderQuery));
            var q = orderQuery.Split(',')[0].TrimStart('-');
            bool desc = orderQuery[0] == '-';
            return (q, !desc);
        }
    }

    //TODO: add comma separated multiple sort params
    public static IEnumerable<T> OrderBySortStringParam<T>(this IEnumerable<T> query, string orderQuery)
    {
        (string field, bool asc) q = ParseMinusNameQuery(orderQuery);
        return q.asc ? query.OrderBy(q.field) : query.OrderByDescending(q.field);
    }
    public static IQueryable<T> OrderBySortStringParam<T>(this IQueryable<T> query, string orderQuery)
    {
        (string field, bool asc) q = ParseMinusNameQuery(orderQuery);
        return q.asc ? query.OrderBy(q.field) : query.OrderByDescending(q.field);
    }

}
