using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Mars.Nodes.Core.Implements.JsonConverters;

/// <summary>
/// ReadOnlySpan on serialize throw error
/// </summary>
public class IgnoreReadOnlySpanPropertiesResolver : DefaultJsonTypeInfoResolver
{
    // Типы ref struct, которые нужно игнорировать
    private static readonly HashSet<string> RefStructTypeNames =
    [
        "ReadOnlySpan`1",
        "Span`1",
        "ReadOnlySequence`1"
    ];

    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        // Если тип уже известен как проблемный - возвращаем пустой контракт
        if (IsRefStruct(type) || typeof(Encoding).IsAssignableFrom(type))
        {
            return JsonTypeInfo.CreateJsonTypeInfo(type, options);
        }

        JsonTypeInfo typeInfo = base.GetTypeInfo(type, options);

        // Удаляем проблемные свойства из нормальных типов
        var propertiesToRemove = typeInfo.Properties
            .Where(p => IsRefStruct(p.PropertyType))
            .ToList();

        foreach (var prop in propertiesToRemove)
        {
            typeInfo.Properties.Remove(prop);
        }

        return typeInfo;
    }

    private static bool IsRefStruct(Type type)
    {
        if (!type.IsValueType) return false;

        // Проверяем по имени (учитывая generic)
        string typeName = type.IsGenericType
            ? type.GetGenericTypeDefinition().Name
            : type.Name;

        return RefStructTypeNames.Contains(typeName);
    }
}
