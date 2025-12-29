using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Shared.Models;

namespace Mars.Shared.JsonConverters;

public class SourceUriJsonConverter : JsonConverter<SourceUri>
{
    public override SourceUri Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"must be string");//Вероятно вы не добавили в JsonSerializerOptions.Converters
        var value = reader.GetString();

        return SourceUri.ConvertFromString(value);
    }

    public override void Write(Utf8JsonWriter writer, SourceUri value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
