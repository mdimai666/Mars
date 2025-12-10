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
using Mars.Host.Shared.Validators;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Services;

internal class PostJsonService : IPostJsonService
{
    private readonly IPostRepository _postRepository;
    private readonly IValidatorFabric _validatorFabric;
    private readonly IMetaFieldMaterializerService _metaFieldMaterializer;
    private readonly IPostService _postService;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IPostTransformer _postTransformer;

    public PostJsonService(
        IPostRepository postRepository,
        IValidatorFabric validatorFabric,
        IMetaFieldMaterializerService metaFieldMaterializer,
        IPostService postService,
        IMetaModelTypesLocator metaModelTypesLocator,
        IPostTransformer postTransformer)
    {
        _postRepository = postRepository;
        _validatorFabric = validatorFabric;
        _metaFieldMaterializer = metaFieldMaterializer;
        _postService = postService;
        _metaModelTypesLocator = metaModelTypesLocator;
        _postTransformer = postTransformer;
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

        var fillDict = await _metaFieldMaterializer.GetFillContext(post.MetaValues, cancellationToken);

        return post?.ToJsonDto(fillDict);
    }

    public async Task<ListDataResult<PostJsonDto>> List(ListPostQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);
        var list = await _postRepository.ListDetail(query, cancellationToken);
        var fillDict = await _metaFieldMaterializer.GetFillContext(list.Items.SelectMany(s => s.MetaValues), cancellationToken);
        return list.ToMap(s => s.ToJsonDtoList(fillDict));
    }

    public async Task<PagingResult<PostJsonDto>> ListTable(ListPostQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);
        var list = await _postRepository.ListTableDetail(query, cancellationToken);
        var fillDict = await _metaFieldMaterializer.GetFillContext(list.Items.SelectMany(s => s.MetaValues), cancellationToken);
        return list.ToMap(s => s.ToJsonDtoList(fillDict));
    }

    public async Task<PostJsonDto> Create(CreatePostJsonQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);

        var postType = _metaModelTypesLocator.GetPostTypeByName(query.Type)
                            ?? throw new NotFoundException($"Post type '{query.Type}' not found");
        var meta = CreateJsonMetaValuesToModifyDto(query.Meta, postType.MetaFields, postType.TypeName);
        var createQuery = ToCreateQuery(query, meta, postType);

        var post = await _postService.Create(createQuery, cancellationToken);

        return (await PostToPostJson(post, renderContent: false, cancellationToken))!;
    }

    public async Task<PostJsonDto> Update(UpdatePostJsonQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);

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
            MetaValues = meta
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
            MetaValues = meta
        };

    internal static IReadOnlyCollection<ModifyMetaValueDetailQuery> CreateJsonMetaValuesToModifyDto(IReadOnlyDictionary<string, JsonValue>? meta,
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
            var jsonVal = meta[key];
            var blank = ModifyMetaValueDetailQuery.GetBlank(mfDict[key]);
            var modified = UpdateMetaValueFromJson(blank, jsonVal);
            appendValues.Add(modified);
        }

        return appendValues;
    }

    internal static IReadOnlyCollection<ModifyMetaValueDetailQuery>? UpdateJsonMetaValuesToModifyDto(IReadOnlyDictionary<string, JsonValue>? meta,
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

        //create new values
        var appendValues = mfDict.Keys.Except(existMetaValues.Select(s => s.MetaField.Key)).Select(key =>
        {
            var jsonVal = meta[key];
            var blank = ModifyMetaValueDetailQuery.GetBlank(mfDict[key]);
            var modified = UpdateMetaValueFromJson(blank, jsonVal);
            return modified;
        }).ToList();

        return existMetaValues.Select(s =>
        {
            var updValue = meta.GetValueOrDefault(s.MetaField.Key);

            if (updValue == null) return s.ToModifyDto();
            else return UpdateMetaValueFromJson(s.ToModifyDto(), updValue);
        }).Concat(appendValues).ToList();
    }

    internal static ModifyMetaValueDetailQuery UpdateMetaValueFromJson(ModifyMetaValueDetailQuery mv, JsonValue jsonValue)
    {
        //if(mv.NULL && jsonValue == null) 

        var t = mv.MetaField.Type;
        if (t == MetaFieldType.Bool)
            return mv with { Bool = jsonValue.GetValue<bool>() };
        else if (t == MetaFieldType.Int)
            return mv with { Int = jsonValue.GetValue<int>() };
        else if (t == MetaFieldType.Float)
            return mv with { Float = jsonValue.GetValue<float>() };
        else if (t == MetaFieldType.Decimal)
            return mv with { Decimal = jsonValue.GetValue<decimal>() };
        else if (t == MetaFieldType.Long)
            return mv with { Long = jsonValue.GetValue<long>() };
        else if (t == MetaFieldType.DateTime)
            return mv with { DateTime = jsonValue.GetValue<DateTime>() };
        else if (t == MetaFieldType.String)
            return mv with { StringShort = jsonValue.GetValue<string>() };
        else if (t == MetaFieldType.Text)
            return mv with { StringText = jsonValue.GetValue<string>() };
        else if (t == MetaFieldType.Relation)
            return mv with { ModelId = jsonValue.GetValue<Guid>() };
        else if (t == MetaFieldType.Select)
            return mv with { VariantId = jsonValue.GetValue<Guid>() };
        else if (t == MetaFieldType.SelectMany)
            return mv with { VariantsIds = jsonValue.GetValue<Guid[]>() };
        else
            throw new NotImplementedException($" casting for '{t}' not implement");
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
                throw new MarsValidationException(new Dictionary<string, string[]>
                {
                    [nameof(CreatePostJsonQuery.Status)] = ["status feature is disabled. status must be empty"]
                });
            }
        }
        return inputStatus;
    }
}
