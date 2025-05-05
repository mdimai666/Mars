using System.Linq.Expressions;
using System.Reflection;

namespace Mars.WebApiClient.Integration.Tests;

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

    public static Expression<Func<T, Guid>> GetIdPropertyExpression<T>()
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, "Id");

        return Expression.Lambda<Func<T, Guid>>(property, parameter);
    }

    public static bool HasGuidIdProperty(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        // Находим все открытые свойства типа Guid
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {
            if (property.Name == "Id" && property.PropertyType == typeof(Guid))
                return true;
        }

        return false;
    }

    public static bool HasGuidIdProperty<T>() => HasGuidIdProperty(typeof(T));

    public static void SetGuidIdProperty(object obj, Guid id)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        Type objectType = obj.GetType();

        // Находим открытое свойство типа Guid с названием "Id"
        PropertyInfo? property = objectType.GetProperty(
            "Id",
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
}
