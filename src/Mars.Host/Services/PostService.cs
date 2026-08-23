using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Mappings.Posts;
using Mars.Host.Shared.Mappings.PostTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.Posts;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Services;

internal class PostService : IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IEventManager _eventManager;
    private readonly IRequestContext _requestContext;
    private readonly IValidatorFactory _validatorFactory;
    private readonly IPostTransformer _postTransformer;
    private readonly IMetaValuesGeneratorService _metaValuesGenerator;

    public PostService(
        IPostRepository postRepository,
        IMetaModelTypesLocator metaModelTypesLocator,
        IEventManager eventManager,
        IRequestContext requestContext,
        IValidatorFactory validatorFactory,
        IPostTransformer postTransformer,
        IMetaValuesGeneratorService metaValuesGenerator)
    {
        _postRepository = postRepository;
        _metaModelTypesLocator = metaModelTypesLocator;
        _eventManager = eventManager;
        _requestContext = requestContext;
        _validatorFactory = validatorFactory;
        _postTransformer = postTransformer;
        _metaValuesGenerator = metaValuesGenerator;
    }

    public Task<PostSummary?> Get(Guid id, CancellationToken cancellationToken)
        => _postRepository.Get(id, cancellationToken);

    public async Task<PostDetail?> GetDetail(Guid id, bool renderContent = true, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetDetail(id, cancellationToken);

        if (!renderContent || post is null) return post;

        return await _postTransformer.Transform(post, cancellationToken);
    }

    public async Task<PostDetail?> GetDetailBySlug(string slug, string type, bool renderContent = true, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetDetailBySlug(slug, type, cancellationToken);
        if (!renderContent || post is null) return post;

        return await _postTransformer.Transform(post, cancellationToken);
    }

    public async Task<ListDataResult<PostSummary>> List(ListPostQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);
        return await _postRepository.List(query, cancellationToken);
    }

    public async Task<PagingResult<PostSummary>> ListTable(ListPostQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);
        return await _postRepository.ListTable(query, cancellationToken);
    }

    public async Task<PostDetail> Create(CreatePostQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);

        var postType = _metaModelTypesLocator.GetPostTypeByName(query.Type);
        if (postType is not null)
        {
            query = StripContentFieldValue(query, postType);
            query = await _metaValuesGenerator.ApplyAsync(postType, query, cancellationToken);
        }

        var id = await _postRepository.Create(query, cancellationToken);
        var created = await GetDetail(id, renderContent: false, cancellationToken);

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostAdd(created.Type), created);//TODO: сделать явный тип.
        _eventManager.TriggerEvent(payload);

        return created;
    }

    public async Task<PostDetail> Update(UpdatePostQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);
        //await _validatorFactory.ValidateAndThrowAsync<UpdatePostQueryValidator, UpdatePostQuery>(query, cancellationToken);

        var postType = _metaModelTypesLocator.GetPostTypeByName(query.Type);
        if (postType is not null)
            query = StripContentFieldValue(query, postType);

        await _postRepository.Update(query, cancellationToken);
        var updated = await GetDetail(query.Id, renderContent: false, cancellationToken);

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostUpdate(updated.Type), updated);
        _eventManager.TriggerEvent(payload);

        return updated;
    }

    /// <summary>
    /// Значения поля контента фичи не хранятся в мета-значениях (значение — колонка
    /// posts.Content): присланные строки такого поля отбрасываются на общем пути записи.
    /// </summary>
    static CreatePostQuery StripContentFieldValue(CreatePostQuery query, PostTypeDetail postType)
    {
        var contentField = postType.ContentField();
        if (contentField is null) return query;

        var values = query.MetaValues.Where(v => v.MetaFieldId != contentField.Id).ToList();
        return values.Count == query.MetaValues.Count ? query : query with { MetaValues = values };
    }

    static UpdatePostQuery StripContentFieldValue(UpdatePostQuery query, PostTypeDetail postType)
    {
        var contentField = postType.ContentField();
        if (contentField is null || query.MetaValues is null) return query;

        var values = query.MetaValues.Where(v => v.MetaFieldId != contentField.Id).ToList();
        return values.Count == query.MetaValues.Count ? query : query with { MetaValues = values };
    }

    public async Task<PostSummary> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync<Guid, DeletePostQueryValidator>(id, cancellationToken);

        var post = await Get(id, cancellationToken) ?? throw new NotFoundException();

        await _postRepository.Delete(id, cancellationToken);

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostDelete(post.Type), post);
        _eventManager.TriggerEvent(payload);
        return post;
    }

    public async Task<IReadOnlyCollection<PostSummary>> DeleteMany(DeleteManyPostQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);

        var posts = await _postRepository.ListAll(new () { Type = null, Ids = query.Ids  }, cancellationToken);

        await _postRepository.DeleteMany(query, cancellationToken);

        foreach (var post in posts)
        {
            var payload = new ManagerEventPayload(_eventManager.Defaults.PostDelete(post.Type), post);
            _eventManager.TriggerEvent(payload);
        }
        return posts;
    }

    #region EDIT_MODEL
    public async Task<PostEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetPostEditDetail(id, cancellationToken) ?? throw new NotFoundException();
        var postType = _metaModelTypesLocator.GetPostTypeByName(post.Type);

        if (post.MetaValues.Count != postType.MetaFields.Count)
        {
            post = post with { MetaValues = EnrichWithBlankMetaValuesFromMetaValues(post.MetaValues, postType.MetaFields, postType.ContentField()?.Key) };
        }

        return new()
        {
            Post = post.ToResponse(),
            PostType = postType.ToResponse()
        };
    }

    public Task<PostEditViewModel> GetEditModelBlank(string type, CancellationToken cancellationToken)
    {
        var postType = _metaModelTypesLocator.GetPostTypeByName(type) ?? throw new NotFoundException();

        var post = GetPostBlank(postType);

        if (post.MetaValues.Count != postType.MetaFields.Count)
        {
            post = post with { MetaValues = EnrichWithBlankMetaValuesFromMetaValues(post.MetaValues, postType.MetaFields, postType.ContentField()?.Key) };
        }

        return Task.FromResult<PostEditViewModel>(new()
        {
            Post = post.ToResponse(),
            PostType = postType.ToResponse()
        });
    }

    /// <summary>
    /// Обогощает незаполненные MataFields
    /// </summary>
    /// <param name="metaValues"></param>
    /// <param name="metaFields"></param>
    /// <param name="contentFieldKey">Поле контента фичи: значения нет в мета-значениях (оно в posts.Content)</param>
    /// <returns></returns>
    public static IReadOnlyCollection<MetaValueDetailDto> EnrichWithBlankMetaValuesFromMetaValues(
                                                            IEnumerable<MetaValueDetailDto> metaValues,
                                                            IReadOnlyCollection<MetaFieldDto> metaFields,
                                                            string? contentFieldKey = null)
    {

        var valuesByMfId = metaValues.GroupBy(s => s.MetaField.Id)
                                     .ToDictionary(g => g.Key, g => g.OrderBy(v => v.Index).ToList());

        var enrichMetaValues = new List<MetaValueDetailDto>(metaFields.Count);

        foreach (var mf in metaFields)
        {
            if (mf.Type == MetaFieldType.Query) continue; // вычислимое — хранимых значений нет
            if (contentFieldKey is not null && mf.Key == contentFieldKey) continue; // значение — в posts.Content

            if (valuesByMfId.TryGetValue(mf.Id, out var values))
            {
                // все строки поля (мульти-значения), без потери дубликатов
                enrichMetaValues.AddRange(values);
            }
            else
            {
                //meta value not set. Create blank
                var blankMetaValue = GetBlankMetaValue(mf);
                enrichMetaValues.Add(blankMetaValue);
            }
        }

        return enrichMetaValues;
    }

    public static MetaValueDetailDto GetBlankMetaValue(MetaFieldDto metaField)
    {
        var def = metaField.Default;
        return new()
        {
            Id = Guid.NewGuid(),
            Index = 0,

            Bool = def?.Bool,
            Int = def?.Int,
            Float = def?.Float,
            Decimal = def?.Decimal,
            Long = def?.Long,
            DateTime = def?.DateTime,
            ModelId = def?.ModelId,
            StringShort = metaField.Type == MetaFieldType.String ? def?.StringShort ?? ""
                        : metaField.Type == MetaFieldType.Select ? metaField.GetDefaultVariantKey()
                        : def?.StringShort,
            StringText = metaField.Type == MetaFieldType.Text ? def?.StringText ?? "" : def?.StringText,
            MetaField = metaField,
            VariantId = def?.VariantId,
            VariantsIds = def?.VariantsIds ?? []
        };
    }

    public PostEditDetail GetPostBlank(PostTypeDetail postType)
    {
        var user = _requestContext.User;

        var isStatusSupport = postType.EnabledFeatures.Contains(PostTypeConstants.Features.Status);
        var status = (isStatusSupport ? postType.PostStatusList.FirstOrDefault()?.Slug : null) ?? "";

        var author = new PostAuthor()
        {
            Id = user?.Id ?? Guid.Empty,
            UserName = user?.UserName ?? "",
            DisplayName = string.Join(' ', Tools.TrimNulls([user?.LastName, user?.FirstName])),
        };

        return new()
        {
            Id = Guid.Empty,
            Slug = "",
            Title = "",
            Content = "",
            Excerpt = "",
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = null,
            LangCode = "",
            Status = status,
            Tags = [],
            Type = postType.TypeName,

            Author = author,
            MetaValues = [],
            CategoryIds = [],
        };
    }
    #endregion

    public CreatePostQuery EnrichQuery(CreatePostRequest request)
    {
        new PostTypeNameValidator(request.Type, _metaModelTypesLocator);
        var postType = _metaModelTypesLocator.GetPostTypeByName(request.Type);

        var createQuery = request.ToQuery(_requestContext.User.Id, postType.MetaFields.ToDictionary(s => s.Id));

        return createQuery;
    }

    public UpdatePostQuery EnrichQuery(UpdatePostRequest request)
    {
        new PostTypeNameValidator(request.Type, _metaModelTypesLocator);
        var postType = _metaModelTypesLocator.GetPostTypeByName(request.Type);

        var updateQuery = request.ToQuery(_requestContext.User.Id, postType.MetaFields.ToDictionary(s => s.Id));

        return updateQuery;
    }

}
