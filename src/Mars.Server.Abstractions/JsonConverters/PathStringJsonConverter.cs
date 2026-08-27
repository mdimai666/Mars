using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace Mars.Host.Shared.JsonConverters;

public class PathStringJsonConverter : JsonConverter<PathString>
{
    public override PathString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new PathString(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, PathString value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
