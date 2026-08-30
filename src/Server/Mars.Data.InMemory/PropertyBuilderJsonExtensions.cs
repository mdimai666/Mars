using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mars.Data.InMemory;

public static class PropertyBuilderJsonExtensions
{
    /// <summary>
    /// Добавляет конвертер для JSON-сериализации/десериализации
    /// для свойства типа TProperty в EF Core.
    /// </summary>
    public static PropertyBuilder<TProperty> HasJsonConversion<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder)
        where TProperty : class, new()
    {
        return propertyBuilder.HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
            v => string.IsNullOrEmpty(v)
                    ? new TProperty()
                    : JsonSerializer.Deserialize<TProperty>(v, (JsonSerializerOptions)null!) ?? new TProperty());
    }

    /// <summary>
    /// InMemory-провайдер не маппит JsonNode (в отличие от Npgsql, где это jsonb) —
    /// свойство хранится как JSON-строка.
    /// </summary>
    public static PropertyBuilder<JsonNode?> HasJsonConversion(this PropertyBuilder<JsonNode?> propertyBuilder)
    {
        return propertyBuilder.HasConversion(
            v => v == null ? null! : v.ToJsonString(),
            v => string.IsNullOrEmpty(v) ? null : JsonNode.Parse(v));
    }
}
