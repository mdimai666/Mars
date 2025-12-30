using System.Linq.Expressions;
using System.Reflection;

namespace Mars.Test.Common.Helpers;

public static class ReflectionHelper
{
    public static Expression<Func<T, bool>> GetIdEqualsExpression<T>(Guid id)
        where T : class
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, "Id");
        var constant = Expression.Constant(id);
        var body = Expression.Equal(property, constant);

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public static Expression<Func<T, Guid>> GetIdPropertyExpression<T>(string prefix = "")
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, prefix + "Id");

        return Expression.Lambda<Func<T, Guid>>(property, parameter);
    }

    public static bool HasGuidIdProperty(Type type, string prefix = "")
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        // Находим все открытые свойства типа Guid
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {
            if (property.Name == prefix + "Id" && property.PropertyType == typeof(Guid))
                return true;
        }

        return false;
    }

    public static bool HasGuidIdProperty<T>(string prefix = "") => HasGuidIdProperty(typeof(T), prefix);

    public static void SetGuidIdProperty(object obj, Guid id, string prefix = "")
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        Type objectType = obj.GetType();

        // Находим открытое свойство типа Guid с названием "Id"
        PropertyInfo? property = objectType.GetProperty(
            prefix + "Id",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            typeof(Guid),
            Type.EmptyTypes,
            null
        );

        if (property != null && property.CanWrite)
        {
            try
            {
                property.SetValue(obj, id);
            }
            catch (TargetInvocationException ex)
            {
                throw new InvalidOperationException($"Не удалось установить значение свойства 'Id' для объекта типа '{objectType.Name}'.", ex.InnerException);
            }
        }
        else
        {
            throw new InvalidOperationException($"Объект типа '{objectType.Name}' не содержит открытого свойства 'Id' типа Guid.");
        }
    }

    public static Expression<Func<T, bool>> GetIdInExpression<T>(Guid[] ids)
        where T : class
    {
        if (ids == null || ids.Length == 0)
            throw new ArgumentException("ids must not be empty", nameof(ids));

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, "Id");

        var containsMethod = typeof(Enumerable)
            .GetMethods()
            .Single(m =>
                m.Name == nameof(Enumerable.Contains) &&
                m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(Guid));

        var idsConstant = Expression.Constant(ids);

        var body = Expression.Call(
            containsMethod,
            idsConstant,
            property
        );

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public static Guid[] SelectIds<T>(IEnumerable<T> entities)
        where T : class
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        var type = typeof(T);

        var idProperty = type.GetProperty("Id");
        if (idProperty == null)
            throw new InvalidOperationException(
                $"Type '{type.Name}' does not contain property 'Id'");

        if (idProperty.PropertyType != typeof(Guid))
            throw new InvalidOperationException(
                $"Property 'Id' of type '{type.Name}' is not Guid");

        return entities
            .Select(e => (Guid)idProperty.GetValue(e)!)
            .ToArray();
    }

    public static Expression<Func<T, bool>> GetAnyByIdsExpression<T>(
        Guid[] ids,
        string idPropertyName = "Id")
        where T : class
    {
        if (ids == null || ids.Length == 0)
            throw new ArgumentException("ids must not be empty", nameof(ids));

        // x =>
        var parameter = Expression.Parameter(typeof(T), "x");

        // x.Id
        var idProperty = Expression.Property(parameter, idPropertyName);

        if (idProperty.Type != typeof(Guid))
            throw new InvalidOperationException(
                $"Property '{idPropertyName}' of '{typeof(T).Name}' must be Guid");

        // ids.Contains(x.Id)
        var containsMethod = typeof(Enumerable)
            .GetMethods()
            .Single(m =>
                m.Name == nameof(Enumerable.Contains) &&
                m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(Guid));

        var idsConstant = Expression.Constant(ids);

        var body = Expression.Call(
            containsMethod,
            idsConstant,
            idProperty
        );

        // x => ids.Contains(x.Id)
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
