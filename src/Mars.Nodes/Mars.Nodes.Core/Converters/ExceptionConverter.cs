using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Core.Extensions;

namespace Mars.Nodes.Core.Converters;

public class ExceptionConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(Exception).IsAssignableFrom(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(ExceptionConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public class ExceptionConverter<TExceptionType> : JsonConverter<TExceptionType>
    where TExceptionType : Exception
{
    public override TExceptionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, TExceptionType value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("Message", value.Message);
        writer.WriteString("StackTrace", value.StackTrace?.TextEllipsis(1000));

        if (value.InnerException != null)
        {
            writer.WritePropertyName("InnerException");
            JsonSerializer.Serialize(writer, value.InnerException, options);
        }

        writer.WriteString("ExceptionType", value.GetType().FullName);

        if (value.Data.Count > 0)
        {
            writer.WritePropertyName("Data");
            JsonSerializer.Serialize(writer, value.Data, options);
        }

        writer.WriteEndObject();
    }
}
