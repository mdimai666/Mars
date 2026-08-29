using System.Globalization;
using System.Text.Json.Nodes;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.MetaFields;
using Mars.Core.Exceptions;

namespace Mars.Cms.Host.Handlers;

/// <summary>
/// Генератор «порядковый номер»: префикс + число с паддингом (пример: ВУ0002).
/// Параметры (Options.generator.params): prefix, paddingWidth, mode ("continue"|"daily"),
/// categoryPrefixes — словарь slug категории → префикс.
/// Скоуп счётчика: префикс (+ "|дата" при ежедневном сбросе).
/// </summary>
internal class SequenceValueGeneratorHandler(IMetaSequenceRepository metaSequenceRepository) : IMetaValueGeneratorHandler
{
    public async Task<object?> GenerateAsync(MetaValueGeneratorContext context, JsonObject? parameters, CancellationToken cancellationToken)
    {
        var field = context.Field;
        if (field.Type != MetaFieldType.String)
            throw MarsValidationException.FromSingleError("generator",
                $"generator 'sequence' requires field type String, not '{field.Type}' (field '{field.Key}')");

        var prefix = ResolvePrefix(parameters, context.CategorySlugs);
        var scopeKey = ScopeKey(prefix, IsDaily(parameters), context.Now);

        var number = await metaSequenceRepository.NextValueAsync(field.Id, scopeKey, cancellationToken);

        return Format(prefix, number, parameters);
    }

    /// <summary>Префикс из категории: первый slug, найденный в словаре, иначе дефолтный префикс</summary>
    public static string ResolvePrefix(JsonObject? parameters, IReadOnlyList<string> categorySlugs)
    {
        var prefix = ReadString(parameters, "prefix") ?? "";

        if (parameters?["categoryPrefixes"] is JsonObject categoryPrefixes)
        {
            foreach (var slug in categorySlugs)
            {
                if (categoryPrefixes[slug] is JsonValue categoryValue && categoryValue.TryGetValue<string>(out var categoryPrefix))
                    return categoryPrefix;
            }
        }

        return prefix;
    }

    /// <summary>Скоуп счётчика: префикс, при ежедневном сбросе + дата момента</summary>
    public static string ScopeKey(string prefix, bool daily, DateTimeOffset moment)
        => daily ? $"{prefix}|{moment:yyyy-MM-dd}" : prefix;

    public static bool IsDaily(JsonObject? parameters)
        => ReadString(parameters, "mode") == MetaFieldGeneratorCatalog.ModeDaily;

    /// <summary>Итоговое значение: префикс + номер с паддингом</summary>
    public static string Format(string prefix, long number, JsonObject? parameters)
    {
        var paddingWidth = parameters?["paddingWidth"] is JsonValue paddingValue && paddingValue.TryGetValue<int>(out var padding) ? padding : 0;
        return prefix + number.ToString(CultureInfo.InvariantCulture).PadLeft(paddingWidth, '0');
    }

    static string? ReadString(JsonObject? parameters, string name)
        => parameters?[name] is JsonValue value && value.TryGetValue<string>(out var result) ? result : null;
}
