using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Mars.Host.Shared.JsonConverters;

public class OrderedPropertiesJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var typeInfo = base.GetTypeInfo(type, options);

        if (typeInfo.Kind == JsonTypeInfoKind.Object)
        {
            foreach (var property in typeInfo.Properties)
            {
                property.Order = OrderedPropertiesConverterCacheHelper.GetOrder(type, property.Name);
            }
        }
        return typeInfo;
    }
}
