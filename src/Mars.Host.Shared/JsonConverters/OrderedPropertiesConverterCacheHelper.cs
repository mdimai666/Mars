using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Mars.Host.Shared.JsonConverters;

public static class OrderedPropertiesConverterCacheHelper
{
    private static ImmutableDictionary<(Type, string), int> propertyOrdersCache =
        ImmutableDictionary<(Type, string), int>.Empty;

    public static int GetOrder(Type propertyOwnerType, string propertyName)
    {
        var normalizedPropertyName = Normalize(propertyName);

        if (propertyOrdersCache.TryGetValue((propertyOwnerType, normalizedPropertyName), out var propertyOrder))
        {
            return propertyOrder;
        }

        var properties = propertyOwnerType.GetProperties();

        var propertyOrders = properties
            .Select(property =>
            {
                var jsonNameAttr = property.GetCustomAttribute<JsonPropertyNameAttribute>();
                var name = jsonNameAttr?.Name ?? property.Name;

                var normalizedName = Normalize(name);

                var key = (propertyOwnerType, normalizedName);

                var order = BaseTypesAndSelf(property.DeclaringType!).Count();

                return new KeyValuePair<(Type, string), int>(key, order);
            })
            .ToImmutableDictionary();

        ImmutableInterlocked.Update(ref propertyOrdersCache, cache => cache.AddRange(propertyOrders));

        if (!propertyOrders.TryGetValue((propertyOwnerType, normalizedPropertyName), out var result))
        {
#if DEBUG
            throw new InvalidOperationException(
                $"Property '{propertyName}' not found for type '{propertyOwnerType.FullName}'");
#else
            return 0;
#endif
        }

        return result;
    }

    private static string Normalize(string name)
        => name.ToLowerInvariant();

    private static IEnumerable<Type> BaseTypesAndSelf(Type t)
    {
        while (t != null)
        {
            yield return t;
            t = t.BaseType!;
        }
    }
}
