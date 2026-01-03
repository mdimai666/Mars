using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Nodes.Core.Fields;

namespace Mars.Nodes.Core.Converters;

public sealed class InputValueJsonConverter<T> : JsonConverter<InputValue<T>>
{
    public override InputValue<T>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("InputValue must be a JSON string.");

        var text = reader.GetString()!;
        return InputValue<T>.Parse(text);
    }

    public override void Write(
        Utf8JsonWriter writer,
        InputValue<T> value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
