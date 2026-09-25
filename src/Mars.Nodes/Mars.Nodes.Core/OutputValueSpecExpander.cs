using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mars.Nodes.Core;

/// <summary>
/// Turns a value type into a flat list of paths a node puts into the message.
/// "[]" in a path means an element of an array.
/// </summary>
public static class OutputValueSpecExpander
{
    public const int DefaultMaxDepth = 3;

    static readonly Type[] NoExpansionTypes =
    [
        typeof(object), typeof(Type), typeof(NodeMsg),
        typeof(JsonElement), typeof(JsonDocument), typeof(JsonNode),
    ];

    static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertiesCache = new();

    public static IReadOnlyList<OutputValueSpec> Expand(string path, Type? type, int maxDepth = DefaultMaxDepth,
                                                        int outputPort = OutputValueSpec.DefaultOutputPort)
    {
        var specs = new List<OutputValueSpec>();
        Walk(specs, path, type, maxDepth, []);
        return [.. specs.Select(s => s with { OutputPort = outputPort })];
    }

    static void Walk(List<OutputValueSpec> specs, string path, Type? type, int depth, HashSet<Type> visited)
    {
        if (string.IsNullOrWhiteSpace(path) || depth < 0) return;

        type = Unwrap(type);
        specs.Add(new OutputValueSpec(path, TypeName(type)));

        if (depth == 0) return;

        if (ElementTypeOf(type) is { } element)
        {
            // Сам элемент массива отдельной точкой не показываем — только его поля под "path[]".
            if (CanDescend(element, visited))
                WalkChildren(specs, $"{path}[]", Unwrap(element)!, depth - 1, visited);
            return;
        }

        if (CanDescend(type, visited))
            WalkChildren(specs, path, type!, depth - 1, visited);
    }

    static void WalkChildren(List<OutputValueSpec> specs, string path, Type type, int depth, HashSet<Type> visited)
    {
        visited.Add(type);

        foreach (var property in Properties(type))
            Walk(specs, $"{path}.{property.Name}", property.PropertyType, depth, visited);

        visited.Remove(type);
    }

    static string TypeName(Type? type)
    {
        var element = ElementTypeOf(type);
        return element is null ? VarNode.GetVarTypeName(type) : $"{VarNode.GetVarTypeName(element)}[]";
    }

    static bool CanDescend(Type? type, HashSet<Type> visited)
    {
        if (type is null || visited.Contains(type)) return false;
        if (NoExpansionTypes.Contains(type) || type.IsEnum) return false;
        if (typeof(IEnumerable).IsAssignableFrom(type)) return false;
        return VarNode.GetVarTypeName(type) == VarNode.ObjectTypeName;
    }

    static Type? ElementTypeOf(Type? type)
    {
        if (type is null || type == typeof(string)) return null;
        if (typeof(IDictionary).IsAssignableFrom(type)) return null;
        if (type.IsArray) return type.GetElementType();

        var enumerable = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerable is null) return null;

        var element = enumerable.GetGenericArguments()[0];
        if (element.IsGenericType && element.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) return null;
        return element;
    }

    static PropertyInfo[] Properties(Type type)
        => PropertiesCache.GetOrAdd(type, static t =>
            [.. t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)]);

    static Type? Unwrap(Type? type)
        => type is null ? null : Nullable.GetUnderlyingType(type) ?? type;
}
