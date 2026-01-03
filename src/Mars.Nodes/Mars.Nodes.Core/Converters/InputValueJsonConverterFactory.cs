using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Nodes.Core.Fields;

namespace Mars.Nodes.Core.Converters;

public sealed class InputValueJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType &&
           typeToConvert.GetGenericTypeDefinition() == typeof(InputValue<>);

    public override JsonConverter CreateConverter(
        Type type,
        JsonSerializerOptions options)
    {
        var t = type.GetGenericArguments()[0];
        var converterType = typeof(InputValueJsonConverter<>).MakeGenericType(t);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
