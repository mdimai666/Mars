using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mars.Core.Features.JsonConverter;

public static class SystemJsonConverter
{
    static JsonSerializerOptions? opt;

    public static System.Text.Json.JsonSerializerOptions DefaultJsonSerializerOptions(bool forceNewInstance = false)
    {
        if (opt == null)
        {

            System.Text.Json.JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true,
            };
            options.Converters.Add(new DateOnlyConverter());
            options.Converters.Add(new DateOnlyNullableConverter());
            options.Converters.Add(new TimeOnlyConverter());
            if (forceNewInstance)
            {
                return options;
            }
            opt = options;
        }

        return opt;
    }

    static JsonSerializerOptions? optNotFormatted;

    public static System.Text.Json.JsonSerializerOptions DefaultJsonSerializerOptionsNotFormatted(bool forceNewInstance = false)
    {
        if (optNotFormatted == null)
        {

            System.Text.Json.JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = false
            };
            options.Converters.Add(new DateOnlyConverter());
            options.Converters.Add(new DateOnlyNullableConverter());
            options.Converters.Add(new TimeOnlyConverter());
            if (forceNewInstance)
            {
                return options;
            }
            optNotFormatted = options;
        }

        return optNotFormatted;
    }
}

public class DateOnlyConverter : JsonConverter<DateOnly>
{
    private readonly string serializationFormat;

    public DateOnlyConverter() : this(null)
    {
    }

    public DateOnlyConverter(string? serializationFormat)
    {
        this.serializationFormat = serializationFormat ?? "yyyy-MM-dd";
    }

    public override DateOnly Read(ref Utf8JsonReader reader,
                            Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is null ? DateOnly.MinValue : DateOnly.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value,
                                        JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(serializationFormat));
}

public class DateOnlyNullableConverter : JsonConverter<DateOnly?>
{
    private readonly string serializationFormat;

    public DateOnlyNullableConverter() : this(null)
    {
    }

    public DateOnlyNullableConverter(string? serializationFormat)
    {
        this.serializationFormat = serializationFormat ?? "yyyy-MM-dd";
    }

    public override DateOnly? Read(ref Utf8JsonReader reader,
                            Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is null ? null : DateOnly.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value,
                                        JsonSerializerOptions options)
        => writer.WriteStringValue(value?.ToString(serializationFormat));
}

public class TimeOnlyConverter : JsonConverter<TimeOnly>
{
    private readonly string serializationFormat;

    public TimeOnlyConverter() : this(null)
    {
    }

    public TimeOnlyConverter(string? serializationFormat)
    {
        this.serializationFormat = serializationFormat ?? "HH:mm:ss.fff";
    }

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return TimeOnly.Parse(value!);
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(serializationFormat));
}
