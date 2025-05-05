using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Host.Shared.Models;

namespace Mars.Host.Shared.JsonConverters;

public class VersionTokenHexJsonConverter : JsonConverter<VersionTokenHex>
{
    public override VersionTokenHex Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"must be string");//Вероятно вы не добавили в JsonSerializerOptions.Converters
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
            throw new JsonException($"argument cannot be empty");

        return VersionTokenHex.FromString(value);
    }

    public override void Write(Utf8JsonWriter writer, VersionTokenHex value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
