using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Contracts.Models;

namespace Mars.Contracts.JsonConverters;

public class SourceUriJsonConverter : JsonConverter<SourceUri?>
{
    public override SourceUri? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"must be string");//Вероятно вы не добавили в JsonSerializerOptions.Converters
        var value = reader.GetString();

        return SourceUri.ConvertFromString(value);
    }

    public override void Write(Utf8JsonWriter writer, SourceUri? value, JsonSerializerOptions options)
    {
        if (value is null || !value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStringValue(value.ToString());
    }
}
