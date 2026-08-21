using System.Text.Json.Nodes;
using HandlebarsDotNet;
using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Core.Utils;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostJsons;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Mappings.PostJsons;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Utils;
using Mars.Host.Shared.Validators;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Services;

internal class PostJsonService : IPostJsonService
{
    private readonly IPostRepository _postRepository;
    private readonly IValidatorFactory _validatorFactory;
    private readonly IMetaFieldMaterializerService _metaFieldMaterializer;
    private readonly IPostService _postService;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IPostTransformer _postTransformer;
    private readonly IMetaQueryFieldResolver _metaQueryFieldResolver;

    public PostJsonService(
        IPostRepository postRepository,
        IValidatorFactory validatorFactory,
        IMetaFieldMaterializerService metaFieldMaterializer,
        IPostService postService,
        IMetaModelTypesLocator metaModelTypesLocator,
        IPostTransformer postTransformer,
        IMetaQueryFieldResolver metaQueryFieldResolver)
    {
        _postRepository = postRepository;
        _validatorFactory = validatorFactory;
        _metaFieldMaterializer = metaFieldMaterializer;
        _postService = postService;
        _metaModelTypesLocator = metaModelTypesLocator;
        _postTransformer = postTransformer;
        _metaQueryFieldResolver = metaQueryFieldResolver;
    }

    public async Task<PostJsonDto?> GetDetail(Guid id, bool renderContent = true, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetDetail(id, cancellationToken);
        if (post == null) return null;

        return await PostToPostJson(post, renderContent, cancellationToken);
    }

    public async Task<PostJsonDto?> GetDetailBySlug(string slug, string type, bool renderContent = true, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetDetailBySlug(slug, type, cancellationToken);
        if (post == null) return null;

        return await PostToPostJson(post, renderContent, cancellationToken);
    }

    private async Task<PostJsonDto?> PostToPostJson(PostDetail post, bool renderContent, CancellationToken cancellationToken)
    {
        if (renderContent) post = await _postTransformer.Transform(post, cancellationToken);

        var fillDict = await _metaFieldMaterializer.GetFillContext(post.MetaValues.Values, cancellationToken);

        var dto = post?.ToJsonDto(fillDict);
        if (dto is not null)
        {
            await FillQueryFieldsAsync([dto], cancellationToken);
        }
        return dto;
    }

    public async Task<ListDataResult<PostJsonDto>> List(ListPostQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);
        var list = await _postRepository.ListDetail(query, cancellationToken);
        var fillDict = await _metaFieldMaterializer.GetFillContext(list.Items.SelectMany(s => s.MetaValues.Values), cancellationToken);
        var result = list.ToMap(s => s.ToJsonDtoList(fillDict));
        await FillQueryFieldsAsync(result.Items, cancellationToken);
        return result;
    }

    public async Task<PagingResult<PostJsonDto>> ListTable(ListPostQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);
        var list = await _postRepository.ListTableDetail(query, cancellationToken);
        var fillDict = await _metaFieldMaterializer.GetFillContext(list.Items.SelectMany(s => s.MetaValues.Values), cancellationToken);
        var result = list.ToMap(s => s.ToJsonDtoList(fillDict));
        await FillQueryFieldsAsync(result.Items, cancellationToken);
        return result;
    }

    /// <summary>
    /// Дописывает вычислимые Query-поля в <see cref="PostJsonDto.Meta"/> (батчем по типам постов)
    /// </summary>
    private async Task FillQueryFieldsAsync(IEnumerable<PostJsonDto> dtos, CancellationToken cancellationToken)
    {
        foreach (var group in dtos.GroupBy(s => s.Type))
        {
            var postType = _metaModelTypesLocator.GetPostTypeByName(group.Key);
            if (postType is null) continue;
            if (!postType.MetaFields.Any(f => f.Type == MetaFieldType.Query)) continue;

            var items = group.ToList();
            var resolved = await _metaQueryFieldResolver.ResolveAsync(postType, items.Select(s => s.Id).ToList(), cancellationToken);
            if (resolved.Count == 0) continue;

            foreach (var dto in items)
            {
                foreach (var (fieldKey, perPost) in resolved)
                {
                    dto.Meta[fieldKey] = perPost.GetValueOrDefault(dto.Id);
                }
            }
        }
    }

    public async Task<PostJsonDto> Create(CreatePostJsonQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);

        var postType = _metaModelTypesLocator.GetPostTypeByName(query.Type)
                            ?? throw new NotFoundException($"Post type '{query.Type}' not found");
        var meta = CreateJsonMetaValuesToModifyDto(query.Meta, postType.MetaFields, postType.TypeName);
        var createQuery = ToCreateQuery(query, meta, postType);

        var post = await _postService.Create(createQuery, cancellationToken);

        return (await PostToPostJson(post, renderContent: false, cancellationToken))!;
    }

    public async Task<PostJsonDto> Update(UpdatePostJsonQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);

        var postType = _metaModelTypesLocator.GetPostTypeByName(query.Type)
                            ?? throw new NotFoundException($"Post type '{query.Type}' not found");
        var existPost = await _postRepository.GetPostEditDetail(query.Id, cancellationToken)
                            ?? throw new NotFoundException($"Post with id '{query.Id}' not found");
        var meta = UpdateJsonMetaValuesToModifyDto(query.Meta, postType.MetaFields, existPost.MetaValues, postType.TypeName);
        var updateQuery = ToUpdateQuery(query, meta, postType);

        var post = await _postService.Update(updateQuery, cancellationToken);

        return (await PostToPostJson(post, renderContent: false, cancellationToken))!;
    }

    CreatePostQuery ToCreateQuery(CreatePostJsonQuery query, IReadOnlyCollection<ModifyMetaValueDetailQuery> meta, PostTypeDetail postType)
        => new()
        {
            Id = query.Id,
            Title = query.Title,
            Slug = query.Slug,
            Status = ResolveStatus(query.Status, postType),
            UserId = query.UserId,
            Tags = query.Tags,
            Content = query.Content,
            Excerpt = query.Excerpt,
            LangCode = query.LangCode,
            Type = query.Type,
            CategoryIds = query.CategoryIds,
            MetaValues = meta,
        };

    UpdatePostQuery ToUpdateQuery(UpdatePostJsonQuery query, IReadOnlyCollection<ModifyMetaValueDetailQuery>? meta, PostTypeDetail postType)
        => new()
        {
            Id = query.Id,
            Title = query.Title,
            Slug = query.Slug,
            Status = ResolveStatus(query.Status, postType),
            UserId = query.UserId,
            Tags = query.Tags,
            Content = query.Content,
            Excerpt = query.Excerpt,
            LangCode = query.LangCode,
            Type = query.Type,
            CategoryIds = query.CategoryIds,
            MetaValues = meta
        };

    internal static IReadOnlyCollection<ModifyMetaValueDetailQuery> CreateJsonMetaValuesToModifyDto(IReadOnlyDictionary<string, JsonNode>? meta,
                                                                                                IReadOnlyCollection<MetaFieldDto> metaFields,
                                                                                                string postTypeName)
    {
        if (meta is null) return [];

        var mfDict = metaFields.ToDictionary(s => s.Key);

        var diff = DiffList.FindDifferences(mfDict.Keys.ToList(), meta.Keys.ToList());

        if (diff.ToAdd.Any())
        {
            throw new InvalidOperationException($"fields '{diff.ToAdd.JoinStr(",")}' not exist for '{postTypeName}'");
        }

        var intersectKeysOnly = meta.Keys.Intersect(mfDict.Keys);
        var appendValues = new List<ModifyMetaValueDetailQuery>();

        foreach (var key in intersectKeysOnly)
        {
            var metaField = mfDict[key];
            if (metaField.Type == MetaFieldType.Query) continue; // вычислимое — значения не принимаются

            var jsonVal = meta[key];
            if (jsonVal is JsonArray array)
            {
                appendValues.AddRange(MultiValuesFromJsonArray(metaField, array));
                continue;
            }

            if (jsonVal is not JsonValue value)
                throw new InvalidOperationException($"value for field '{key}' of '{postTypeName}' has unsupported json shape");

            var blank = ModifyMetaValueDetailQuery.GetBlank(metaField);
            appendValues.Add(MetaFieldUtils.MetaValueFromJson(blank, value));
        }

        return appendValues;
    }

    /// <summary>
    /// Мульти-значения Relation/File/Image: массив ИД → строки значения с порядком
    /// </summary>
    static IEnumerable<ModifyMetaValueDetailQuery> MultiValuesFromJsonArray(MetaFieldDto metaField, JsonArray array)
    {
        if (metaField.Type is not (MetaFieldType.Relation or MetaFieldType.File or MetaFieldType.Image))
            throw new InvalidOperationException($"array value supported only for Relation/File/Image fields, not '{metaField.Type}'");

        var index = 0;
        foreach (var element in array)
        {
            if (element is not JsonValue elementValue) continue;

            var blank = ModifyMetaValueDetailQuery.GetBlank(metaField);
            yield return MetaFieldUtils.MetaValueFromJson(blank, elementValue) with { Index = index };
            index++;
        }
    }

    internal static IReadOnlyCollection<ModifyMetaValueDetailQuery>? UpdateJsonMetaValuesToModifyDto(IReadOnlyDictionary<string, JsonNode>? meta,
                                                                                                IReadOnlyCollection<MetaFieldDto> metaFields,
                                                                                                IReadOnlyCollection<MetaValueDetailDto> existMetaValues,
                                                                                                string postTypeName)
    {
        if (meta is null) return null;

        var mfDict = metaFields.ToDictionary(s => s.Key);

        var diff = DiffList.FindDifferences(mfDict.Keys.ToList(), meta.Keys.ToList());

        if (diff.ToAdd.Any())
        {
            throw new InvalidOperationException($"fields '{diff.ToAdd.JoinStr(",")}' not exist for '{postTypeName}'");
        }

        // мульти-значения Relation/File/Image заменяются целиком (старые строки уходят в диффе)
        var multiKeys = mfDict.Where(kv => kv.Value.Type is MetaFieldType.Relation or MetaFieldType.File or MetaFieldType.Image
                                        && meta.GetValueOrDefault(kv.Key) is JsonArray)
                              .Select(kv => kv.Key)
                              .ToHashSet();

        var existFieldKeys = existMetaValues.Select(s => s.MetaField.Key).ToHashSet();

        // create new values + замена мульти-значений
        var appendValues = new List<ModifyMetaValueDetailQuery>();
        foreach (var key in mfDict.Keys.Except(existFieldKeys).Concat(multiKeys).Distinct())
        {
            var metaField = mfDict[key];
            if (metaField.Type == MetaFieldType.Query) continue; // вычислимое — значения не принимаются

            var jsonVal = meta.GetValueOrDefault(key);
            if (jsonVal is null) continue;

            if (jsonVal is JsonArray array)
            {
                appendValues.AddRange(MultiValuesFromJsonArray(metaField, array));
            }
            else if (jsonVal is JsonValue value)
            {
                var blank = ModifyMetaValueDetailQuery.GetBlank(metaField);
                appendValues.Add(MetaFieldUtils.MetaValueFromJson(blank, value));
            }
            else
            {
                throw new InvalidOperationException($"value for field '{key}' of '{postTypeName}' has unsupported json shape");
            }
        }

        return existMetaValues
            .Where(s => s.MetaField.Type != MetaFieldType.Query && !multiKeys.Contains(s.MetaField.Key))
            .Select(s =>
            {
                var updValue = meta.GetValueOrDefault(s.MetaField.Key) as JsonValue;

                if (updValue == null) return s.ToModifyDto();
                else return MetaFieldUtils.MetaValueFromJson(s.ToModifyDto(), updValue);
            }).Concat(appendValues).ToList();
    }

    private string? ResolveStatus(string? inputStatus, PostTypeDetail postType)
    {
        if (postType.EnabledFeatures.Contains(PostTypeConstants.Features.Status))
        {
            if (string.IsNullOrEmpty(inputStatus))
            {
                return postType.PostStatusList.FirstOrDefault()?.Slug;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(inputStatus))
            {
                throw MarsValidationException.FromSingleError(nameof(CreatePostJsonQuery.Status), "status feature is disabled. status must be empty");
            }
        }
        return inputStatus;
    }
}
