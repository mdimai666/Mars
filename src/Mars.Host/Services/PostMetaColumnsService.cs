using System.Globalization;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.MetaFields;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Services;

internal class PostMetaColumnsService : IPostMetaColumnsService
{
    private readonly MarsDbContext _marsDbContext;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IServiceProvider _serviceProvider;

    public PostMetaColumnsService(MarsDbContext marsDbContext,
                                  IMetaModelTypesLocator metaModelTypesLocator,
                                  IServiceProvider serviceProvider)
    {
        _marsDbContext = marsDbContext;
        _metaModelTypesLocator = metaModelTypesLocator;
        _serviceProvider = serviceProvider;
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string?>>> GetDisplayValuesAsync(
        string typeName,
        IReadOnlyCollection<string> fieldKeys,
        IReadOnlyCollection<Guid> postIds,
        CancellationToken cancellationToken = default)
    {
        var empty = new Dictionary<Guid, IReadOnlyDictionary<string, string?>>();
        if (postIds.Count == 0 || fieldKeys.Count == 0) return empty;

        var postType = _metaModelTypesLocator.GetPostTypeByName(typeName);
        if (postType is null) return empty;

        var fields = postType.MetaFields
                             .Where(f => fieldKeys.Contains(f.Key) && f.Type != MetaFieldType.Query)
                             .ToList();
        if (fields.Count == 0) return empty;

        var fieldIds = fields.Select(f => f.Id).ToList();
        var values = await _marsDbContext.PostMetaValues
                                         .Where(v => postIds.Contains(v.PostId) && fieldIds.Contains(v.MetaFieldId))
                                         .OrderBy(v => v.Index)
                                         .ToListAsync(cancellationToken);

        var relationTitles = await LoadRelationTitlesAsync(fields, values, cancellationToken);

        var result = new Dictionary<Guid, IReadOnlyDictionary<string, string?>>();
        foreach (var postId in postIds)
        {
            var row = new Dictionary<string, string?>();
            foreach (var field in fields)
            {
                var fieldValues = values.Where(v => v.PostId == postId && v.MetaFieldId == field.Id).ToList();
                row[field.Key] = FormatField(field, fieldValues, relationTitles);
            }
            result[postId] = row;
        }

        return result;
    }

    /// <summary>Батчем подгружает заголовки целей связей по всем полям-отношениям</summary>
    async Task<Dictionary<Guid, string>> LoadRelationTitlesAsync(List<MetaFieldDto> fields,
                                                                 List<PostMetaValueEntity> values,
                                                                 CancellationToken cancellationToken)
    {
        var titles = new Dictionary<Guid, string>();
        foreach (var field in fields.Where(f => f.IsTypeRelation && !string.IsNullOrEmpty(f.ModelName)))
        {
            var ids = values.Where(v => v.MetaFieldId == field.Id && v.ModelId is not null)
                            .Select(v => v.ModelId!.Value)
                            .Distinct()
                            .ToArray();
            if (ids.Length == 0) continue;

            var rootModelName = field.ModelName!.Split('.', 2)[0];
            var provider = _metaModelTypesLocator.GetMetaRelationModelProvider(rootModelName, _serviceProvider);
            if (provider is null) continue;

            var summaries = await provider.GetIds(field.ModelName, ids, cancellationToken);
            foreach (var summary in summaries.Values)
            {
                titles.TryAdd(summary.Id, summary.Title);
            }
        }

        return titles;
    }

    static string? FormatField(MetaFieldDto field, List<PostMetaValueEntity> fieldValues, IReadOnlyDictionary<Guid, string> relationTitles)
    {
        if (fieldValues.Count == 0) return null;
        var first = fieldValues[0];

        switch (field.Type)
        {
            case MetaFieldType.String: return first.StringShort;
            case MetaFieldType.Text: return first.StringText;
            case MetaFieldType.Bool: return first.Bool?.ToString().ToLowerInvariant();
            case MetaFieldType.Int: return first.Int?.ToString(CultureInfo.InvariantCulture);
            case MetaFieldType.Long: return first.Long?.ToString(CultureInfo.InvariantCulture);
            case MetaFieldType.Float: return first.Float?.ToString(CultureInfo.InvariantCulture);
            case MetaFieldType.Decimal: return first.Decimal?.ToString(CultureInfo.InvariantCulture);
            case MetaFieldType.DateTime: return first.DateTime?.ToString("g", CultureInfo.InvariantCulture);

            case MetaFieldType.Select:
            {
                var variant = field.Variants?.FirstOrDefault(v => v.Id == first.VariantId);
                return variant is null ? null : VariantDisplay(variant);
            }

            case MetaFieldType.SelectMany:
            {
                var variantTitles = (first.VariantsIds ?? [])
                    .Select(id => field.Variants?.FirstOrDefault(v => v.Id == id))
                    .Where(v => v is not null)
                    .Select(v => VariantDisplay(v!));
                return string.Join(", ", variantTitles);
            }

            case MetaFieldType.Relation:
            case MetaFieldType.File:
            case MetaFieldType.Image:
                return string.Join(", ", fieldValues
                    .Where(v => v.ModelId is not null)
                    .Select(v => relationTitles.GetValueOrDefault(v.ModelId!.Value))
                    .Where(t => t is not null));

            default:
                return null;
        }
    }

    static string VariantDisplay(MetaFieldVariantDto variant)
        => string.IsNullOrEmpty(variant.Title) ? variant.Key : variant.Title;
}
