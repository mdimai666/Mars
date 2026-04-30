using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mars.Nodes.Core.Implements.JsonConverters;

//public class ExceptionConverter<TExceptionType> : JsonConverter<TExceptionType>
//{
//    public override bool CanConvert(Type typeToConvert)
//    {
//        return typeof(Exception).IsAssignableFrom(typeToConvert);
//    }

//    public override TExceptionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//    {
//        throw new NotSupportedException("Deserializing exceptions is not allowed");
//    }

//    public override void Write(Utf8JsonWriter writer, TExceptionType value, JsonSerializerOptions options)
//    {
//        var serializableProperties = value.GetType()
//            .GetProperties()
//            .Select(uu => new { uu.Name, Value = uu.GetValue(value) })
//            .Where(uu => uu.Name != nameof(Exception.TargetSite));

//        if (options?.IgnoreNullValues == true)
//        {
//            serializableProperties = serializableProperties.Where(uu => uu.Value != null);
//        }

//        var propList = serializableProperties.ToList();

//        if (propList.Count == 0)
//        {
//            // Nothing to write
//            return;
//        }

//        writer.WriteStartObject();

//        foreach (var prop in propList)
//        {
//            writer.WritePropertyName(prop.Name);
//            JsonSerializer.Serialize(writer, prop.Value, options);
//        }

//        writer.WriteEndObject();
//    }
//}

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
    {
        //using var jsonDoc = JsonDocument.ParseValue(ref reader);
        //var root = jsonDoc.RootElement;

        //var message = root.GetProperty("Message").GetString();
        //var stackTrace = root.GetProperty("StackTrace").GetString();
        //var innerException = root.TryGetProperty("InnerException", out var innerElement) && innerElement.ValueKind != JsonValueKind.Null
        //    ? JsonSerializer.Deserialize<Exception>(innerElement.GetRawText(), options)
        //    : null;

        //// Создаем исключение через Activator (если конструктор принимает message и innerException)
        //var exception = (TExceptionType)Activator.CreateInstance(
        //    typeof(TExceptionType),
        //    message,
        //    innerException);

        //// Установка StackTrace через reflection (так как свойство обычно read-only)
        //if (!string.IsNullOrEmpty(stackTrace))
        //{
        //    typeof(Exception).GetField("_stackTrace", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
        //        ?.SetValue(exception, stackTrace);
        //}

        //return exception;
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, TExceptionType value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("Message", value.Message);
        if(false)
            writer.WriteString("StackTrace", value.StackTrace);

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
