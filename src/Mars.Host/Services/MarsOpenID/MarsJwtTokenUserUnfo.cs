using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mars.Host.Services.MarsSSOClient;

public class MarsJwtTokenUserUnfo
{
    [JsonPropertyName("exp")]
    public long Expire { get; set; }

    [JsonPropertyName("iss")]
    public string Issuer { get; init; } = default!;

    [JsonPropertyName("aud")]
    public string aud { get; init; } = default!;

    [JsonPropertyName(ClaimTypes.NameIdentifier)]
    public Guid Id { get; init; }

    [JsonPropertyName(ClaimTypes.Email)]
    public string Email { get; init; } = default!;

    [JsonPropertyName(ClaimTypes.Name)]
    public string Username { get; init; } = default!;

    [JsonPropertyName(ClaimTypes.GivenName)]
    public string FirstName { get; init; } = default!;

    [JsonPropertyName(ClaimTypes.Surname)]
    public string LastName { get; init; } = default!;

    [JsonConverter(typeof(StringOrArrayStringConverter))]
    [JsonPropertyName(ClaimTypes.Role)]
    public IReadOnlyCollection<string> Role { get; init; } = default!; //TODO: Role может быит и строкой. Пофиксить
}

public class StringOrArrayStringConverter : JsonConverter<IReadOnlyCollection<string>>
{
    public override IReadOnlyCollection<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            return [reader.GetString()];
        }
        //return reader.Read<IReadOnlyCollection<string>>(options);
        //return reader.Deserialize(typeToConvert, options) as IReadOnlyCollection<string>;
        return JsonSerializer.Deserialize(ref reader, typeToConvert, options) as IReadOnlyCollection<string>;
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyCollection<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        JsonSerializer.Serialize(writer, value, options);
        writer.WriteEndArray();
    }
}
