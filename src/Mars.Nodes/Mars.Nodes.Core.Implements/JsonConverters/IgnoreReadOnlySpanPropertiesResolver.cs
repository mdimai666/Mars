using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Mars.Nodes.Core.Implements.JsonConverters;

/// <summary>
/// ReadOnlySpan on serialize throw error
/// </summary>
public class IgnoreReadOnlySpanPropertiesResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        if (type.IsGenericType && (type.Name == "ReadOnlySequence`1"))
        {
            var jt = JsonTypeInfo.CreateJsonTypeInfo(type, options);
            jt.CreateObject = Array.Empty<int>;
            return jt;
        }

        JsonTypeInfo typeInfo = base.GetTypeInfo(type, options);

        foreach (JsonPropertyInfo property in typeInfo.Properties)
        {
            if (property.PropertyType.IsGenericType
                //&& property.PropertyType.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>)
                && property.PropertyType.Name == "ReadOnlySequence`1"
                )
            {
                //property.Name = "xxx";
                //property.ShouldSerialize = (_, _) => false;
            }
        }

        return typeInfo;
    }
}
