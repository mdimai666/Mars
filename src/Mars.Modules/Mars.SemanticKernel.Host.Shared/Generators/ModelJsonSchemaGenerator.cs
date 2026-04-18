using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mars.SemanticKernel.Host.Shared.Generators;

public class ModelJsonSchemaGenerator
{
    private readonly JsonSerializerOptions _jsonOptions;

    public ModelJsonSchemaGenerator()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public string GenerateSchema<T>() => GenerateSchema(typeof(T));

    public string GenerateSchema(Type type)
    {
        var schema = BuildJsonSchema(type);
        return JsonSerializer.Serialize(schema, _jsonOptions);
    }

    private Dictionary<string, object> BuildJsonSchema(Type type)
    {
        var schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>(),
            ["required"] = new List<string>(),
            ["additionalProperties"] = false
        };

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var requiredProps = new List<string>();
        var propsDict = (Dictionary<string, object>)schema["properties"];

        foreach (var prop in properties)
        {
            var propName = GetPropertyName(prop);
            var propSchema = GetPropertySchema(prop);
            propsDict[propName] = propSchema;

            if (IsRequired(prop))
                requiredProps.Add(propName);
        }

        schema["required"] = requiredProps;

        // Добавляем описание модели
        var description = GetTypeDescription(type);
        if (!string.IsNullOrEmpty(description))
            schema["description"] = description;

        return schema;
    }

    private object GetPropertySchema(PropertyInfo prop)
    {
        var propType = prop.PropertyType;
        var isNullable = Nullable.GetUnderlyingType(propType) != null;
        var actualType = Nullable.GetUnderlyingType(propType) ?? propType;

        var schema = new Dictionary<string, object>();

        // Определяем тип
        if (actualType == typeof(string))
        {
            schema["type"] = "string";
            AddStringConstraints(prop, schema);
        }
        else if (actualType == typeof(int) || actualType == typeof(long) ||
                 actualType == typeof(short) || actualType == typeof(byte))
        {
            schema["type"] = "integer";
            AddNumericConstraints(prop, schema);
        }
        else if (actualType == typeof(decimal) || actualType == typeof(double) ||
                 actualType == typeof(float))
        {
            schema["type"] = "number";
            AddNumericConstraints(prop, schema);
        }
        else if (actualType == typeof(bool))
        {
            schema["type"] = "boolean";
        }
        else if (actualType == typeof(DateTime))
        {
            schema["type"] = "string";
            schema["format"] = "date-time";
        }
        else if (actualType == typeof(DateOnly))
        {
            schema["type"] = "string";
            schema["format"] = "date";
        }
        else if (actualType == typeof(TimeOnly))
        {
            schema["type"] = "string";
            schema["format"] = "time";
        }
        else if (actualType == typeof(Guid))
        {
            schema["type"] = "string";
            schema["format"] = "uuid";
        }
        else if (actualType.IsEnum)
        {
            schema["type"] = "string";
            schema["enum"] = Enum.GetNames(actualType);
        }
        //else if (actualType.IsArray || (actualType.IsGenericType &&
        //         actualType.GetGenericTypeDefinition() == typeof(List<>)))
        //{
        //    schema["type"] = "array";
        //    var itemType = actualType.IsArray
        //        ? actualType.GetElementType()
        //        : actualType.GetGenericArguments()[0];
        //    schema["items"] = BuildJsonSchema(itemType);
        //}
        else if (actualType.IsArray || (actualType.IsGenericType &&
             actualType.GetGenericTypeDefinition() == typeof(List<>)))
        {
            schema["type"] = "array";
            var itemType = actualType.IsArray
                ? actualType.GetElementType()
                : actualType.GetGenericArguments()[0];

            // 🔥 КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: для примитивных типов не строим объект
            schema["items"] = GetPrimitiveItemSchema(itemType);
        }
        else if (actualType.IsClass && actualType != typeof(string))
        {
            // Вложенный объект
            return BuildJsonSchema(actualType);
        }

        // Добавляем описание из атрибута
        var description = GetPropertyDescription(prop);
        if (!string.IsNullOrEmpty(description))
            schema["description"] = description;

        // Добавляем пример
        var example = GetPropertyExample(prop);
        if (example != null)
            schema["example"] = example;

        if (isNullable)
            schema["nullable"] = true;

        return schema;
    }

    private void AddStringConstraints(PropertyInfo prop, Dictionary<string, object> schema)
    {
        var maxLength = prop.GetCustomAttribute<MaxLengthAttribute>();
        if (maxLength != null)
            schema["maxLength"] = maxLength.Length;

        var minLength = prop.GetCustomAttribute<MinLengthAttribute>();
        if (minLength != null)
            schema["minLength"] = minLength.Length;

        var regex = prop.GetCustomAttribute<RegularExpressionAttribute>();
        if (regex != null)
            schema["pattern"] = regex.Pattern;
    }

    private void AddNumericConstraints(PropertyInfo prop, Dictionary<string, object> schema)
    {
        var range = prop.GetCustomAttribute<RangeAttribute>();
        if (range != null)
        {
            schema["minimum"] = Convert.ToDecimal(range.Minimum);
            schema["maximum"] = Convert.ToDecimal(range.Maximum);
        }
    }

    private string GetPropertyName(PropertyInfo prop)
    {
        var jsonAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (jsonAttr != null)
            return jsonAttr.Name;

        var displayAttr = prop.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>();
        if (displayAttr != null && !string.IsNullOrEmpty(displayAttr.Name))
            return displayAttr.Name;

        return JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
    }

    private bool IsRequired(PropertyInfo prop)
    {
        var requiredAttr = prop.GetCustomAttribute<RequiredAttribute>();
        if (requiredAttr != null)
            return true;

        // Для record с positional parameters все свойства required по умолчанию
        var declaringType = prop.DeclaringType;
        if (declaringType != null && IsRecordType(declaringType))
        {
            var constructor = declaringType.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Any(p => p.Name == prop.Name.ToLower()));
            if (constructor != null)
                return true;
        }

        return false;
    }

    private string GetPropertyDescription(PropertyInfo prop)
    {
        var descAttr = prop.GetCustomAttribute<DescriptionAttribute>();
        if (descAttr != null)
            return descAttr.Description;

        var displayAttr = prop.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>();
        if (displayAttr != null && !string.IsNullOrEmpty(displayAttr.Description))
            return displayAttr.Description;

        return null;
    }

    private object GetPropertyExample(PropertyInfo prop)
    {
        var exampleAttr = prop.GetCustomAttribute<ExampleAttribute>();
        if (exampleAttr != null)
            return exampleAttr.Example;

        // Генерируем пример на основе типа
        var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        if (propType == typeof(string)) return "string value";
        if (propType == typeof(int)) return 0;
        if (propType == typeof(long)) return 0L;
        if (propType == typeof(decimal)) return 0.0m;
        if (propType == typeof(bool)) return false;
        if (propType == typeof(DateTime)) return DateTime.Now.ToString("o");
        if (propType == typeof(Guid)) return Guid.NewGuid().ToString();

        return null;
    }

    private string GetTypeDescription(Type type)
    {
        var descAttr = type.GetCustomAttribute<DescriptionAttribute>();
        if (descAttr != null)
            return descAttr.Description;

        return null;
    }

    private bool IsRecordType(Type type)
    {
        return type.GetMethod("<Clone>$") != null;
    }

    private object GetPrimitiveItemSchema(Type itemType)
    {
        // Если это string
        if (itemType == typeof(string))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "string"
            };
        }

        // Если это числовой тип
        if (itemType == typeof(int) || itemType == typeof(long) ||
            itemType == typeof(short) || itemType == typeof(byte))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "integer"
            };
        }

        // Если это decimal/double/float
        if (itemType == typeof(decimal) || itemType == typeof(double) ||
            itemType == typeof(float))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "number"
            };
        }

        // Если это bool
        if (itemType == typeof(bool))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "boolean"
            };
        }

        // Если это enum
        if (itemType.IsEnum)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "string",
                ["enum"] = Enum.GetNames(itemType)
            };
        }

        // Для сложных типов - строим полную схему объекта
        return BuildJsonSchema(itemType);
    }
}

// Вспомогательные атрибуты для улучшения схемы
[AttributeUsage(AttributeTargets.Property)]
public class ExampleAttribute : Attribute
{
    public object Example { get; }
    public ExampleAttribute(object example) => Example = example;
}

//[AttributeUsage(AttributeTargets.Property)]
//public class DescriptionAttribute : Attribute
//{
//    public string Description { get; }
//    public DescriptionAttribute(string description) => Description = description;
//}
