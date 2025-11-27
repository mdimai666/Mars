using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mars.Host.Data.InMemory;

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
}
