using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mars.Nodes.Core.Implements.Utils;

public static class EndpointJsonSchemaTool
{
    static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        AllowTrailingCommas = true
    };

    static readonly JsonNodeOptions _jsonNodeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public sealed class SimpleJsonSchema
    {
        public required string Type { get; init; } // object, array, string, number, boolean
        public Dictionary<string, SimpleJsonSchema>? Properties { get; init; }
        public HashSet<string>? Required { get; init; }
        public SimpleJsonSchema? Items { get; init; }
    }

    public sealed record JsonValidationResult
    {
        public required bool IsValid { get; init; }
        public JsonNode? ValidatedJson { get; init; }
        public required IReadOnlyList<string> Errors { get; init; }
    }

    public static JsonValidationResult ValidateAndFilter(
        SimpleJsonSchema schema,
        JsonNode input)
    {
        var errors = new List<string>();

        var filtered = ValidateNode(schema, input, "$", errors);

        return new JsonValidationResult
        {
            IsValid = errors.Count == 0,
            ValidatedJson = errors.Count == 0 ? filtered : null,
            Errors = errors
        };
    }

    public static JsonValidationResult ValidateAndFilter(
        SimpleJsonSchema schema,
        string json)
    {
        var node = JsonNode.Parse(json, _jsonNodeOptions)
            ?? throw new ArgumentException("Invalid JSON");

        return ValidateAndFilter(schema, node);
    }

    public static JsonValidationResult ValidateAndFilter(
        string schemaJson,
        string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaJson, nameof(schemaJson));
        var schema = JsonSerializer.Deserialize<SimpleJsonSchema>(
            schemaJson,
            _jsonSerializerOptions)
            ?? throw new ArgumentException("Invalid schema JSON");

        var node = JsonNode.Parse(json, _jsonNodeOptions)
            ?? throw new ArgumentException("Invalid JSON");

        return ValidateAndFilter(schema, node);
    }

    public static JsonValidationResult ValidateAndFilter(
        string schemaJson,
        JsonNode jsonNode)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaJson, nameof(schemaJson));
        var schema = JsonSerializer.Deserialize<SimpleJsonSchema>(
            schemaJson,
            _jsonSerializerOptions)
            ?? throw new ArgumentException("Invalid schema JSON");

        return ValidateAndFilter(schema, jsonNode);
    }

    private static JsonNode? ValidateNode(
        SimpleJsonSchema schema,
        JsonNode node,
        string path,
        List<string> errors)
    {
        if (!ValidateType(schema.Type, node))
        {
            errors.Add($"{path}: invalid type");
            return null;
        }

        if (node is JsonObject obj && schema.Type == "object")
        {
            var result = new JsonObject();

            if (schema.Required != null)
            {
                foreach (var required in schema.Required)
                {
                    if (!obj.ContainsKey(required))
                        errors.Add($"{path}.{required}: required");
                }
            }

            if (schema.Properties != null)
            {
                foreach (var (name, propSchema) in schema.Properties)
                {
                    if (!obj.TryGetPropertyValue(name, out var value) || value is null)
                        continue;

                    var filtered = ValidateNode(
                        propSchema,
                        value,
                        $"{path}.{name}",
                        errors);

                    if (filtered is not null)
                        result[name] = filtered;
                }
            }

            return result;
        }

        if (node is JsonArray arr && schema.Type == "array")
        {
            if (schema.Items is null)
            {
                errors.Add($"{path}: items schema missing");
                return null;
            }

            var result = new JsonArray();
            int i = 0;

            foreach (var item in arr)
            {
                var filtered = ValidateNode(
                    schema.Items,
                    item!,
                    $"{path}[{i++}]",
                    errors);

                if (filtered is not null)
                    result.Add(filtered);
            }

            return result;
        }

        // primitives
        return node.DeepClone();
    }

    private static bool ValidateType(string schemaType, JsonNode node) =>
        schemaType switch
        {
            "object" => node is JsonObject,
            "array" => node is JsonArray,
            "string" => node is JsonValue v && v.TryGetValue<string>(out _),
            "number" => node is JsonValue v && v.TryGetValue<double>(out _),
            "integer" => node is JsonValue v && v.TryGetValue<int>(out _),
            "boolean" => node is JsonValue v && v.TryGetValue<bool>(out _),
            _ => false
        };
}
