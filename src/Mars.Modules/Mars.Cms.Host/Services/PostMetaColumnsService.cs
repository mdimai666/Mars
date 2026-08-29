using System.Globalization;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.MetaFields;
using Mars.Data.Contexts;
using Mars.Data.Entities;
using Mars.Data.Repositories.Helpers;
using Mars.Options.Services;
using Microsoft.EntityFrameworkCore;

namespace Mars.Cms.Host.Services;

internal class PostMetaColumnsService : IPostMetaColumnsService
{
    private readonly MarsDbContext _marsDbContext;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionService _optionService;

    public PostMetaColumnsService(MarsDbContext marsDbContext,
                                  IMetaModelTypesLocator metaModelTypesLocator,
                                  IServiceProvider serviceProvider,
                                  IOptionService optionService)
    {
        _marsDbContext = marsDbContext;
        _metaModelTypesLocator = metaModelTypesLocator;
        _serviceProvider = serviceProvider;
        _optionService = optionService;
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

        var contentFieldKey = postType.ContentField()?.Key;
        var fields = postType.MetaFields
                             .Where(f => fieldKeys.Contains(f.Key) && f.Type != MetaFieldType.Query && f.Key != contentFieldKey)
                             .ToList();
        if (fields.Count == 0) return empty;

        var fieldIds = fields.Select(f => f.Id).ToList();
        var values = await _marsDbContext.PostMetaValues
                                         .Where(v => postIds.Contains(v.PostId) && fieldIds.Contains(v.MetaFieldId))
                                         .OrderBy(v => v.Index)
                                         .ToListAsync(cancellationToken);

        var relationTitles = await LoadRelationTitlesAsync(fields, values, cancellationToken);
        var filePreviews = await LoadFilePreviewsAsync(fields, values, cancellationToken);

        var result = new Dictionary<Guid, IReadOnlyDictionary<string, string?>>();
        foreach (var postId in postIds)
        {
            var row = new Dictionary<string, string?>();
            foreach (var field in fields)
            {
                var fieldValues = values.Where(v => v.PostId == postId && v.MetaFieldId == field.Id).ToList();
                row[field.Key] = FormatField(field, fieldValues, relationTitles, filePreviews);
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

    async Task<Dictionary<Guid, string>> LoadFilePreviewsAsync(List<MetaFieldDto> fields,
                                                               List<PostMetaValueEntity> values,
                                                               CancellationToken cancellationToken)
    {
        var previews = new Dictionary<Guid, string>();

        var fileFields = fields.Where(f => f.Type is MetaFieldType.File or MetaFieldType.Image).ToList();
        if (fileFields.Count == 0) return previews;

        var fieldIds = fileFields.Select(f => f.Id).ToList();
        var fileIds = values.Where(v => fieldIds.Contains(v.MetaFieldId) && v.ModelId is not null)
                            .Select(v => v.ModelId!.Value)
                            .Distinct()
                            .ToArray();
        if (fileIds.Length == 0) return previews;

        var resolver = new ImagePreviewResolver(new(), _optionService.FileHostingInfo());
        var files = await _marsDbContext.Files.AsNoTracking()
                                              .Where(f => fileIds.Contains(f.Id))
                                              .ToListAsync(cancellationToken);
        foreach (var file in files)
        {
            previews.TryAdd(file.Id, resolver.ResolvePreview(file));
        }

        return previews;
    }

    static string? FormatField(MetaFieldDto field,
                               List<PostMetaValueEntity> fieldValues,
                               IReadOnlyDictionary<Guid, string> relationTitles,
                               IReadOnlyDictionary<Guid, string> filePreviews)
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
                return string.Join(", ", fieldValues
                    .Where(v => v.ModelId is not null)
                    .Select(v => relationTitles.GetValueOrDefault(v.ModelId!.Value))
                    .Where(t => t is not null));

            case MetaFieldType.File:
            case MetaFieldType.Image:
                return string.Join(", ", fieldValues
                    .Where(v => v.ModelId is not null)
                    .Select(v => filePreviews.GetValueOrDefault(v.ModelId!.Value))
                    .Where(t => t is not null));

            default:
                return null;
        }
    }

    static string VariantDisplay(MetaFieldVariantDto variant)
        => string.IsNullOrEmpty(variant.Title) ? variant.Key : variant.Title;
}
